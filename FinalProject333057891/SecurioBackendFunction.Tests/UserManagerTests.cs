using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for UserManager: GetProfileAsync, UpdateUserAsync,
    // DeleteUserAsync, GetPasswordHistoryAsync.
    public class UserManagerTests
    {
        static UserManagerTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        private const int UserId = 42;
        private const string SafeHash  = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        private const string PwnedHash = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        private static User ProfileUser() => new User
        {
            Id       = UserId,
            Username = "alice",
            Email    = "alice@example.com"
        };

        private static User OldUser() => new User
        {
            Id                = UserId,
            MasterPasswordKey = "oldkey==",
            AuthSalt          = "oldsalt==",
            LastPasswordUpdate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        private static User ValidUpdate(bool withPassword = false, string? sha1 = null) => new User
        {
            Username          = "updateduser",
            Email             = "updated@example.com",
            MasterPasswordKey = withPassword ? "newkey==" : null,
            AuthSalt          = withPassword ? "newsalt==" : null,
            EncryptionSalt    = withPassword ? "newencsalt==" : null,
            PasswordSha1Hash  = sha1
        };

        private static UserManager Build(
            Mock<IUserRepository> userRepo,
            Mock<IVaultItemRepository>? vaultRepo = null,
            Mock<IHibpService>? hibp = null)
        {
            if (hibp == null)
            {
                hibp = new Mock<IHibpService>();
                // Default: no password is pwned unless the test supplies its own mock.
                hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);
            }

            userRepo.Setup(r => r.GetUserByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(OldUser());
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
        public async Task GetProfile_ExistingUser_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(UserId)).ReturnsAsync(ProfileUser());

            var result = await Build(repo).GetProfileAsync(UserId);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task GetProfile_ExistingUser_ReturnsUserData()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(UserId)).ReturnsAsync(ProfileUser());

            var result = await Build(repo).GetProfileAsync(UserId);

            Assert.NotNull(result.Data);
            Assert.Equal(UserId, result.Data!.Id);
            Assert.Equal("alice", result.Data.Username);
        }

        [Fact]
        public async Task GetProfile_UserNotFound_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(UserId)).ReturnsAsync((User)null);

            var result = await Build(repo).GetProfileAsync(UserId);

            Assert.False(result.Success);
            Assert.Equal("Profile not found.", result.Message);
        }

        // ── UpdateUserAsync: validation ───────────────────────────────────────────

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateUser_EmptyUsername_ReturnsFail(string? username)
        {
            var repo = new Mock<IUserRepository>();
            var user = ValidUpdate();
            user.Username = username!;

            var result = await Build(repo).UpdateUserAsync(UserId, user, false);

            Assert.False(result.Success);
            Assert.Contains("Username", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateUser_EmptyEmail_ReturnsFail(string? email)
        {
            var repo = new Mock<IUserRepository>();
            var user = ValidUpdate();
            user.Email = email!;

            var result = await Build(repo).UpdateUserAsync(UserId, user, false);

            Assert.False(result.Success);
            Assert.Contains("Email", result.Message);
        }

        [Fact]
        public async Task UpdateUser_EmailAlreadyTaken_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), UserId))
                .ReturnsAsync(true);

            var result = await Build(repo).UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.False(result.Success);
            Assert.Contains("in use", result.Message);
        }

        // ── UpdateUserAsync: password change required fields ──────────────────────

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UpdateUser_PasswordChanged_MissingMasterKey_ReturnsFail(string? key)
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            var user = ValidUpdate(withPassword: true);
            user.MasterPasswordKey = key!;

            var result = await Build(repo).UpdateUserAsync(UserId, user, true);

            Assert.False(result.Success);
            Assert.Contains("MasterPasswordKey", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UpdateUser_PasswordChanged_MissingAuthSalt_ReturnsFail(string? salt)
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            var user = ValidUpdate(withPassword: true);
            user.AuthSalt = salt!;

            var result = await Build(repo).UpdateUserAsync(UserId, user, true);

            Assert.False(result.Success);
            Assert.Contains("AuthSalt", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UpdateUser_PasswordChanged_MissingEncryptionSalt_ReturnsFail(string? salt)
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            var user = ValidUpdate(withPassword: true);
            user.EncryptionSalt = salt!;

            var result = await Build(repo).UpdateUserAsync(UserId, user, true);

            Assert.False(result.Success);
            Assert.Contains("EncryptionSalt", result.Message);
        }

        // ── UpdateUserAsync: HIBP check on password change ────────────────────────

        [Fact]
        public async Task UpdateUser_PasswordChanged_PwnedHash_ReturnsFail()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            var user = ValidUpdate(withPassword: true, sha1: PwnedHash);

            var result = await Build(repo, hibp: hibp).UpdateUserAsync(UserId, user, true);

            Assert.False(result.Success);
            Assert.Contains("data breach", result.Message);
        }

        [Fact]
        public async Task UpdateUser_PasswordChanged_NoSha1_HibpNotCalled()
        {
            var hibp = new Mock<IHibpService>();
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            var user = ValidUpdate(withPassword: true, sha1: null);

            await Build(repo, hibp: hibp).UpdateUserAsync(UserId, user, true);

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        // ── UpdateUserAsync: happy path ───────────────────────────────────────────

        [Fact]
        public async Task UpdateUser_NoPasswordChange_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), false)).ReturnsAsync(true);

            var result = await Build(repo).UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.True(result.Success);
            Assert.Equal("Account updated successfully.", result.Message);
        }

        [Fact]
        public async Task UpdateUser_WithPasswordChange_ArchivesOldPassword()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);

            await Build(repo).UpdateUserAsync(UserId, ValidUpdate(withPassword: true), true);

            repo.Verify(r => r.AddPasswordHistoryAsync(
                UserId,
                OldUser().MasterPasswordKey,
                OldUser().AuthSalt,
                It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_WithPasswordChange_BulkUpdateVaultItems()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.BulkUpdateVaultItemsAsync(It.IsAny<List<VaultItem>>(), UserId))
                .ReturnsAsync(true);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), true)).ReturnsAsync(true);

            var reEncryptedItems = new List<VaultItem> { new VaultItem { Id = 1 } };

            await Build(repo, vaultRepo).UpdateUserAsync(UserId, ValidUpdate(withPassword: true), true, reEncryptedItems);

            vaultRepo.Verify(r => r.BulkUpdateVaultItemsAsync(It.IsAny<List<VaultItem>>(), UserId), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_NoPasswordChange_DoesNotArchive()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), false)).ReturnsAsync(true);

            await Build(repo).UpdateUserAsync(UserId, ValidUpdate(), false);

            repo.Verify(r => r.AddPasswordHistoryAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateUser_RepositoryReturnsFalse_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            var result = await Build(repo).UpdateUserAsync(UserId, ValidUpdate(), false);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        // ── DeleteUserAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_ExistingUser_ReturnsSuccess()
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

        // ── GetPasswordHistoryAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetPasswordHistory_ReturnsHistoryList()
        {
            var history = new List<MasterPasswordHistory>
            {
                new MasterPasswordHistory { Id = 1, UserId = UserId, PasswordKey = "old1", AuthSalt = "salt1" },
                new MasterPasswordHistory { Id = 2, UserId = UserId, PasswordKey = "old2", AuthSalt = "salt2" }
            };

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetLastPasswordHistoryAsync(UserId, 4)).ReturnsAsync(history);

            var result = await Build(repo).GetPasswordHistoryAsync(UserId);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);
        }

        [Fact]
        public async Task GetPasswordHistory_EmptyHistory_ReturnsEmptyList()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetLastPasswordHistoryAsync(UserId, 4))
                .ReturnsAsync(new List<MasterPasswordHistory>());

            var result = await Build(repo).GetPasswordHistoryAsync(UserId);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetPasswordHistory_FetchesMaxFourEntries()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetLastPasswordHistoryAsync(UserId, 4))
                .ReturnsAsync(new List<MasterPasswordHistory>());

            await Build(repo).GetPasswordHistoryAsync(UserId);

            repo.Verify(r => r.GetLastPasswordHistoryAsync(UserId, 4), Times.Once);
        }
    }
}
