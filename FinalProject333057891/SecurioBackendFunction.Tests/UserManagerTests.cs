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
    // Comprehensive unit tests for UserManager.
    // Covers GetProfileAsync, UpdateUserAsync (all validation paths), and DeleteUserAsync.
    // All repository and HIBP dependencies are mocked.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class UserManagerTests
    {
        private const int UserId = 42;

        private const string SafeHash  = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        private const string PwnedHash = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        private static User ValidUpdate(bool withPassword = false, string? sha1Hash = null) => new User
        {
            Username          = "updateduser",
            Email             = "updated@example.com",
            MasterPasswordKey = withPassword ? "newkey==" : null,
            AuthSalt          = withPassword ? "newsalt==" : null,
            EncryptionSalt    = withPassword ? "newencsalt==" : null,
            PasswordSha1Hash  = sha1Hash
        };

        private static UserManager Build(
            Mock<IUserRepository> userRepo,
            Mock<IVaultItemRepository>? vaultRepo = null,
            Mock<IHibpService>? hibp = null)
        {
            if (hibp == null)
            {
                hibp = new Mock<IHibpService>();
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

        // ── GetProfileAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetProfile_UserExists_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(UserId)).ReturnsAsync(new User
            {
                Id       = UserId,
                Username = "alice",
                Email    = "alice@example.com"
            });

            var result = await Build(repo).GetProfileAsync(UserId);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task GetProfile_UserExists_DataContainsUser()
        {
            var storedUser = new User { Id = UserId, Username = "alice", Email = "alice@example.com" };
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(UserId)).ReturnsAsync(storedUser);

            var result = await Build(repo).GetProfileAsync(UserId);

            Assert.NotNull(result.Data);
            Assert.Equal(UserId, result.Data!.Id);
            Assert.Equal("alice", result.Data.Username);
        }

        [Fact]
        public async Task GetProfile_UserNotFound_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(UserId)).ReturnsAsync((User?)null);

            var result = await Build(repo).GetProfileAsync(UserId);

            Assert.False(result.Success);
            Assert.Equal("Profile not found.", result.Message);
        }

        // ── UpdateUserAsync: username validation ──────────────────────────────────

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateUser_InvalidUsername_ReturnsFail(string? username)
        {
            var repo = new Mock<IUserRepository>();
            var user = ValidUpdate();
            user.Username = username!;

            var result = await Build(repo).UpdateUserAsync(UserId, user, false);

            Assert.False(result.Success);
            Assert.Contains("Username", result.Message);
        }

        // ── UpdateUserAsync: email validation ─────────────────────────────────────

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateUser_InvalidEmail_ReturnsFail(string? email)
        {
            var repo = new Mock<IUserRepository>();
            var user = ValidUpdate();
            user.Email = email!;

            var result = await Build(repo).UpdateUserAsync(UserId, user, false);

            Assert.False(result.Success);
            Assert.Contains("Email", result.Message);
        }

        // ── UpdateUserAsync: email uniqueness ─────────────────────────────────────

        [Fact]
        public async Task UpdateUser_EmailTakenByOtherUser_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync("updated@example.com", UserId)).ReturnsAsync(true);

            var result = await Build(repo).UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.False(result.Success);
            Assert.Contains("already in use", result.Message);
        }

        [Fact]
        public async Task UpdateUser_EmailTakenByOtherUser_DatabaseUpdateNotCalled()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);

            await Build(repo).UpdateUserAsync(UserId, ValidUpdate(), false);

            repo.Verify(r => r.UpdateUserAsync(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
        }

        // ── UpdateUserAsync: password change validation ───────────────────────────

        [Fact]
        public async Task UpdateUser_PasswordChangedButMissingKey_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);

            var user = ValidUpdate(withPassword: false);
            var result = await Build(repo).UpdateUserAsync(UserId, user, passwordChanged: true);

            Assert.False(result.Success);
            Assert.Contains("MasterPasswordKey", result.Message);
        }

        [Fact]
        public async Task UpdateUser_PasswordChangedButMissingAuthSalt_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);

            var user = ValidUpdate(withPassword: true);
            user.AuthSalt = "";
            var result = await Build(repo).UpdateUserAsync(UserId, user, passwordChanged: true);

            Assert.False(result.Success);
            Assert.Contains("AuthSalt", result.Message);
        }

        [Fact]
        public async Task UpdateUser_PasswordChangedButMissingEncSalt_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);

            var user = ValidUpdate(withPassword: true);
            user.EncryptionSalt = "";
            var result = await Build(repo).UpdateUserAsync(UserId, user, passwordChanged: true);

            Assert.False(result.Success);
            Assert.Contains("EncryptionSalt", result.Message);
        }

        // ── UpdateUserAsync: HIBP breach check ───────────────────────────────────

        [Fact]
        public async Task UpdateUser_PwnedPassword_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var result = await Build(repo, hibp: hibp)
                .UpdateUserAsync(UserId, ValidUpdate(withPassword: true, sha1Hash: PwnedHash), passwordChanged: true);

            Assert.False(result.Success);
            Assert.Contains("data breach", result.Message);
        }

        [Fact]
        public async Task UpdateUser_PwnedPassword_DatabaseUpdateNotCalled()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            await Build(repo, hibp: hibp)
                .UpdateUserAsync(UserId, ValidUpdate(withPassword: true, sha1Hash: PwnedHash), passwordChanged: true);

            repo.Verify(r => r.UpdateUserAsync(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUser_SafePassword_HibpCalledOnce()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            await Build(repo, hibp: hibp)
                .UpdateUserAsync(UserId, ValidUpdate(withPassword: true, sha1Hash: SafeHash), passwordChanged: true);

            hibp.Verify(h => h.IsPasswordPwnedAsync(SafeHash), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task UpdateUser_NullOrEmptyHash_SkipsHibpCheck(string? hash)
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            var hibp = new Mock<IHibpService>();

            await Build(repo, hibp: hibp)
                .UpdateUserAsync(UserId, ValidUpdate(withPassword: true, sha1Hash: hash), passwordChanged: true);

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        // ── UpdateUserAsync: successful paths ────────────────────────────────────

        [Fact]
        public async Task UpdateUser_ValidUsernameAndEmail_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), false)).ReturnsAsync(true);

            var result = await Build(repo).UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.True(result.Success);
            Assert.Contains("updated successfully", result.Message);
        }

        [Fact]
        public async Task UpdateUser_SetsUserIdFromParameter()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.Is<User>(u => u.Id == UserId), false)).ReturnsAsync(true);

            var result = await Build(repo).UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.True(result.Success);
            repo.Verify(r => r.UpdateUserAsync(It.Is<User>(u => u.Id == UserId), false), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_PasswordChangeWithVaultItems_BulkUpdatesVault()
        {
            var repo      = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            vaultRepo.Setup(v => v.BulkUpdateVaultItemsAsync(It.IsAny<List<VaultItem>>(), UserId)).ReturnsAsync(true);

            var items = new List<VaultItem>
            {
                new VaultItem { Id = 1, IV = "iv1", Tag = "tag1", CipherText = "ct1" },
                new VaultItem { Id = 2, IV = "iv2", Tag = "tag2", CipherText = "ct2" }
            };

            var result = await Build(repo, vaultRepo)
                .UpdateUserAsync(UserId, ValidUpdate(withPassword: true), true, items);

            Assert.True(result.Success);
            vaultRepo.Verify(v => v.BulkUpdateVaultItemsAsync(items, UserId), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_PasswordChangeWithEmptyVault_DoesNotCallBulkUpdate()
        {
            var repo      = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);

            var result = await Build(repo, vaultRepo)
                .UpdateUserAsync(UserId, ValidUpdate(withPassword: true), true, new List<VaultItem>());

            Assert.True(result.Success);
            vaultRepo.Verify(v => v.BulkUpdateVaultItemsAsync(It.IsAny<List<VaultItem>>(), It.IsAny<int>()), Times.Never);
        }

        // ── UpdateUserAsync: repository failures ─────────────────────────────────

        [Fact]
        public async Task UpdateUser_RepoUpdateReturnsFalse_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), false)).ReturnsAsync(false);

            var result = await Build(repo).UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task UpdateUser_VaultBulkUpdateFails_ReturnsFail()
        {
            var repo      = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);
            vaultRepo.Setup(v => v.BulkUpdateVaultItemsAsync(It.IsAny<List<VaultItem>>(), UserId)).ReturnsAsync(false);

            var items = new List<VaultItem>
            {
                new VaultItem { Id = 1, IV = "iv1", Tag = "tag1", CipherText = "ct1" }
            };

            var result = await Build(repo, vaultRepo)
                .UpdateUserAsync(UserId, ValidUpdate(withPassword: true), true, items);

            Assert.False(result.Success);
            Assert.Contains("re-encryption failed", result.Message);
        }

        // ── DeleteUserAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_UserExists_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.DeleteUserAsync(UserId)).ReturnsAsync(true);

            var result = await Build(repo).DeleteUserAsync(UserId);

            Assert.True(result.Success);
            Assert.Equal("Account deleted successfully.", result.Message);
        }

        [Fact]
        public async Task DeleteUser_UserNotFound_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.DeleteUserAsync(UserId)).ReturnsAsync(false);

            var result = await Build(repo).DeleteUserAsync(UserId);

            Assert.False(result.Success);
            Assert.Equal("Account not found.", result.Message);
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
