using Moq;
using Xunit;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for UserManager.UpdateUserAsync.
    // All dependencies are mocked — no real database calls are made.
    public class UserManagerUpdateTests
    {
        private const int UserId = 42;

        // Valid SHA-1 hex string used as a stand-in for a non-leaked password hash.
        private const string SafeHash  = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        // Valid SHA-1 hex string used as a stand-in for a leaked password hash.
        private const string PwnedHash = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        private static User ValidUpdate(bool withPassword = false, string sha1Hash = null) => new User
        {
            Username = "updateduser",
            Email = "updated@example.com",
            MasterPasswordKey = withPassword ? "newkey==" : null,
            AuthSalt = withPassword ? "newsalt==" : null,
            EncryptionSalt = withPassword ? "newencsalt==" : null,
            PasswordSha1Hash = sha1Hash
        };

        private static UserManager Build(
            Mock<IUserRepository> userRepo,
            Mock<IVaultItemRepository> vaultRepo = null,
            Mock<IHibpService> hibp = null)
        {
            if (hibp == null)
            {
                hibp = new Mock<IHibpService>();
                // Default: no password is pwned unless the test supplies its own mock.
                hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);
            }

            // Default: GetUserByIdAsync returns a valid user so password-change tests that
            // reach the archival step don't fail with a null-reference on the old user.
            userRepo.Setup(r => r.GetUserByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new User
                {
                    Id = UserId,
                    MasterPasswordKey = "oldkey==",
                    AuthSalt = "oldsalt==",
                    LastPasswordUpdate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });

            // Default: AddPasswordHistoryAsync is a no-op.
            userRepo.Setup(r => r.AddPasswordHistoryAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            return new UserManager(
                userRepo.Object,
                (vaultRepo ?? new Mock<IVaultItemRepository>()).Object,
                hibp.Object);
        }

        // ── Username validation ──────────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserAsync_EmptyUsername_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            var mgr = Build(repo);
            var user = ValidUpdate();
            user.Username = "";

            var result = await mgr.UpdateUserAsync(UserId, user, false);

            Assert.False(result.Success);
            Assert.Contains("Username", result.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_NullUsername_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            var mgr = Build(repo);
            var user = ValidUpdate();
            user.Username = null;

            var result = await mgr.UpdateUserAsync(UserId, user, false);

            Assert.False(result.Success);
            Assert.Contains("Username", result.Message);
        }

        // ── Email validation ─────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserAsync_EmptyEmail_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            var mgr = Build(repo);
            var user = ValidUpdate();
            user.Email = "";

            var result = await mgr.UpdateUserAsync(UserId, user, false);

            Assert.False(result.Success);
            Assert.Contains("Email", result.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_NullEmail_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            var mgr = Build(repo);
            var user = ValidUpdate();
            user.Email = null;

            var result = await mgr.UpdateUserAsync(UserId, user, false);

            Assert.False(result.Success);
            Assert.Contains("Email", result.Message);
        }

        // ── Email uniqueness ─────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserAsync_EmailTakenByOtherUser_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync("updated@example.com", UserId))
                .ReturnsAsync(true);
            var mgr = Build(repo);

            var result = await mgr.UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.False(result.Success);
            Assert.Contains("already in use", result.Message);
        }

        // ── Password change validation ───────────────────────────────────────────

        [Fact]
        public async Task UpdateUserAsync_PasswordChangedButMissingKey_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            var mgr = Build(repo);
            var user = ValidUpdate(withPassword: false);

            var result = await mgr.UpdateUserAsync(UserId, user, passwordChanged: true);

            Assert.False(result.Success);
            Assert.Contains("MasterPasswordKey", result.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_PasswordChangedButMissingAuthSalt_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            var mgr = Build(repo);
            var user = ValidUpdate(withPassword: true);
            user.AuthSalt = "";

            var result = await mgr.UpdateUserAsync(UserId, user, passwordChanged: true);

            Assert.False(result.Success);
            Assert.Contains("AuthSalt", result.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_PasswordChangedButMissingEncSalt_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            var mgr = Build(repo);
            var user = ValidUpdate(withPassword: true);
            user.EncryptionSalt = "";

            var result = await mgr.UpdateUserAsync(UserId, user, passwordChanged: true);

            Assert.False(result.Success);
            Assert.Contains("EncryptionSalt", result.Message);
        }

        // ── HIBP breach check ────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserAsync_PwnedPassword_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            var hibp = new Mock<IHibpService>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);
            var mgr = Build(repo, hibp: hibp);

            var user = ValidUpdate(withPassword: true, sha1Hash: PwnedHash);
            var result = await mgr.UpdateUserAsync(UserId, user, passwordChanged: true);

            Assert.False(result.Success);
            Assert.Contains("data breach", result.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_PwnedPassword_DoesNotCallRepo()
        {
            var repo = new Mock<IUserRepository>();
            var hibp = new Mock<IHibpService>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);
            var mgr = Build(repo, hibp: hibp);

            var user = ValidUpdate(withPassword: true, sha1Hash: PwnedHash);
            await mgr.UpdateUserAsync(UserId, user, passwordChanged: true);

            repo.Verify(r => r.UpdateUserAsync(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUserAsync_SafePassword_PassesHibpAndSucceeds()
        {
            var repo = new Mock<IUserRepository>();
            var hibp = new Mock<IHibpService>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var mgr = Build(repo, hibp: hibp);

            var user = ValidUpdate(withPassword: true, sha1Hash: SafeHash);
            var result = await mgr.UpdateUserAsync(UserId, user, passwordChanged: true);

            Assert.True(result.Success);
            hibp.Verify(h => h.IsPasswordPwnedAsync(SafeHash), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_NullPasswordHash_SkipsHibpCheck()
        {
            var repo = new Mock<IUserRepository>();
            var hibp = new Mock<IHibpService>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            var mgr = Build(repo, hibp: hibp);

            // No hash provided — HIBP check must be skipped entirely.
            var user = ValidUpdate(withPassword: true, sha1Hash: null);
            var result = await mgr.UpdateUserAsync(UserId, user, passwordChanged: true);

            Assert.True(result.Success);
            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUserAsync_EmptyPasswordHash_SkipsHibpCheck()
        {
            var repo = new Mock<IUserRepository>();
            var hibp = new Mock<IHibpService>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            var mgr = Build(repo, hibp: hibp);

            var user = ValidUpdate(withPassword: true, sha1Hash: "");
            var result = await mgr.UpdateUserAsync(UserId, user, passwordChanged: true);

            Assert.True(result.Success);
            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        // ── Successful updates ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserAsync_ValidUsernameAndEmail_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), false)).ReturnsAsync(true);
            var mgr = Build(repo);

            var result = await mgr.UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.True(result.Success);
            Assert.Contains("updated successfully", result.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_ValidPasswordChange_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            vaultRepo.Setup(v => v.BulkUpdateVaultItemsAsync(It.IsAny<List<VaultItem>>(), UserId)).ReturnsAsync(true);
            var mgr = Build(repo, vaultRepo);

            var items = new List<VaultItem>
            {
                new VaultItem { Id = 1, IV = "iv1", Tag = "tag1", CipherText = "ct1" },
                new VaultItem { Id = 2, IV = "iv2", Tag = "tag2", CipherText = "ct2" }
            };

            var result = await mgr.UpdateUserAsync(UserId, ValidUpdate(withPassword: true), true, items);

            Assert.True(result.Success);
            vaultRepo.Verify(v => v.BulkUpdateVaultItemsAsync(items, UserId), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_PasswordChangeWithEmptyVault_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            var mgr = Build(repo, vaultRepo);

            var result = await mgr.UpdateUserAsync(UserId, ValidUpdate(withPassword: true), true, new List<VaultItem>());

            Assert.True(result.Success);
            vaultRepo.Verify(v => v.BulkUpdateVaultItemsAsync(It.IsAny<List<VaultItem>>(), It.IsAny<int>()), Times.Never);
        }

        // ── Repository failure ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserAsync_RepoUpdateFails_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), false)).ReturnsAsync(false);
            var mgr = Build(repo);

            var result = await mgr.UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_VaultBulkUpdateFails_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            vaultRepo.Setup(v => v.BulkUpdateVaultItemsAsync(It.IsAny<List<VaultItem>>(), UserId)).ReturnsAsync(false);
            var mgr = Build(repo, vaultRepo);

            var items = new List<VaultItem>
            {
                new VaultItem { Id = 1, IV = "iv1", Tag = "tag1", CipherText = "ct1" }
            };

            var result = await mgr.UpdateUserAsync(UserId, ValidUpdate(withPassword: true), true, items);

            Assert.False(result.Success);
            Assert.Contains("re-encryption failed", result.Message);
        }

        // ── UserId binding ───────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserAsync_SetsUserIdFromParameter()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.Is<User>(u => u.Id == UserId), false)).ReturnsAsync(true);
            var mgr = Build(repo);

            var result = await mgr.UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.True(result.Success);
            repo.Verify(r => r.UpdateUserAsync(It.Is<User>(u => u.Id == UserId), false), Times.Once);
        }

        // ── Password history archival ─────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserAsync_PasswordChanged_ArchivesOldPasswordToHistory()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            var mgr = Build(repo);

            await mgr.UpdateUserAsync(UserId, ValidUpdate(withPassword: true), passwordChanged: true);

            // The old key and salt fetched via GetUserByIdAsync must be persisted to history exactly once.
            repo.Verify(r => r.AddPasswordHistoryAsync(UserId, "oldkey==", "oldsalt==", It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_PasswordNotChanged_DoesNotArchiveHistory()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), false)).ReturnsAsync(true);
            var mgr = Build(repo);

            await mgr.UpdateUserAsync(UserId, ValidUpdate(), passwordChanged: false);

            repo.Verify(r => r.AddPasswordHistoryAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUserAsync_PasswordChanged_FetchesOldUserBeforeUpdate()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            var mgr = Build(repo);

            await mgr.UpdateUserAsync(UserId, ValidUpdate(withPassword: true), passwordChanged: true);

            repo.Verify(r => r.GetUserByIdAsync(UserId), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_PasswordChanged_OldUserNotFound_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            // Override the default Build() setup: simulate user not found.
            repo.Setup(r => r.GetUserByIdAsync(It.IsAny<int>())).ReturnsAsync((User)null);
            var mgr = Build(repo);

            var result = await mgr.UpdateUserAsync(UserId, ValidUpdate(withPassword: true), passwordChanged: true);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task GetPasswordHistoryAsync_ReturnsLastFourEntries()
        {
            var repo = new Mock<IUserRepository>();
            var history = new List<MasterPasswordHistory>
            {
                new MasterPasswordHistory { Id = 1, UserId = UserId, PasswordKey = "key1", AuthSalt = "salt1", CreatedAt = DateTime.UtcNow.AddMonths(-1) },
                new MasterPasswordHistory { Id = 2, UserId = UserId, PasswordKey = "key2", AuthSalt = "salt2", CreatedAt = DateTime.UtcNow.AddMonths(-2) },
            };
            repo.Setup(r => r.GetLastPasswordHistoryAsync(UserId, 4)).ReturnsAsync(history);
            var mgr = Build(repo);

            var result = await mgr.GetPasswordHistoryAsync(UserId);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data.Count);
            repo.Verify(r => r.GetLastPasswordHistoryAsync(UserId, 4), Times.Once);
        }
    }
}
