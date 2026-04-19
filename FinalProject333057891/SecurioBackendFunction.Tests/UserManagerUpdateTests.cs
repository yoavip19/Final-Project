using Moq;
using Xunit;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;
using System.Collections.Generic;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for UserManager.UpdateUserAsync.
    // All dependencies are mocked — no real database calls are made.
    public class UserManagerUpdateTests
    {
        private const int UserId = 42;

        private static User ValidUpdate(bool withPassword = false) => new User
        {
            Username = "updateduser",
            Email = "updated@example.com",
            MasterPasswordKey = withPassword ? "newkey==" : null,
            AuthSalt = withPassword ? "newsalt==" : null,
            EncryptionSalt = withPassword ? "newencsalt==" : null
        };

        private static UserManager Build(Mock<IUserRepository> userRepo, Mock<IVaultItemRepository> vaultRepo = null)
            => new UserManager(userRepo.Object, (vaultRepo ?? new Mock<IVaultItemRepository>()).Object);

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
    }
}
