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
    // Unit tests for PasswordCheckManager.
    // All dependencies are mocked — no database or network calls are made.
    public class PasswordCheckManagerTests
    {
        private static VaultItem MakeItem(string sha1, DateTime lastUpdate) => new VaultItem
        {
            Id = 1,
            UserId = 1,
            AccountName = "Site",
            Sha1Hash = sha1,
            LastUpdate = lastUpdate
        };

        private static User MakeUser(DateTime lastPasswordUpdate) => new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            LastPasswordUpdate = lastPasswordUpdate
        };

        private (Mock<IVaultItemRepository> vaultRepo, Mock<IUserRepository> userRepo, Mock<IHibpService> hibp) CreateMocks()
        {
            return (new Mock<IVaultItemRepository>(), new Mock<IUserRepository>(), new Mock<IHibpService>());
        }

        // ── Validation ──────────────────────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Check_InvalidUserId_ReturnsFailure(int userId)
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(userId);

            Assert.False(result.Success);
            Assert.Equal("Invalid user ID.", result.Message);
        }

        // ── Empty vault ─────────────────────────────────────────────────

        [Fact]
        public async Task Check_EmptyVault_ReturnsZeroCounts()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data.BreachedCount);
            Assert.Equal(0, result.Data.OldCount);
            Assert.False(result.Data.MasterPasswordOld);
        }

        // ── Breached passwords ──────────────────────────────────────────

        [Fact]
        public async Task Check_OneBreachedPassword_ReturnsBreachedCountOne()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            var sha1 = "5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8";
            var items = new List<VaultItem> { MakeItem(sha1, DateTime.UtcNow) };
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            hibp.Setup(h => h.IsPasswordPwnedAsync(sha1)).ReturnsAsync(true);
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data.BreachedCount);
        }

        [Fact]
        public async Task Check_NoBreachedPasswords_ReturnsBreachedCountZero()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            var sha1 = "ABCD1234567890ABCD1234567890ABCD12345678";
            var items = new List<VaultItem> { MakeItem(sha1, DateTime.UtcNow) };
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            hibp.Setup(h => h.IsPasswordPwnedAsync(sha1)).ReturnsAsync(false);
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data.BreachedCount);
        }

        [Fact]
        public async Task Check_ItemWithEmptySha1_SkipsHibpCheck()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            var items = new List<VaultItem> { MakeItem("", DateTime.UtcNow) };
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data.BreachedCount);
            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never());
        }

        // ── Old passwords ───────────────────────────────────────────────

        [Fact]
        public async Task Check_PasswordOlderThan90Days_ReturnsOldCountOne()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            var items = new List<VaultItem> { MakeItem("ABCD1234567890ABCD1234567890ABCD12345678", DateTime.UtcNow.AddDays(-100)) };
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data.OldCount);
        }

        [Fact]
        public async Task Check_RecentPassword_ReturnsOldCountZero()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            var items = new List<VaultItem> { MakeItem("ABCD1234567890ABCD1234567890ABCD12345678", DateTime.UtcNow.AddDays(-10)) };
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data.OldCount);
        }

        [Fact]
        public async Task Check_PasswordWithDefaultLastUpdate_NotCountedAsOld()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            var items = new List<VaultItem> { MakeItem("ABCD1234567890ABCD1234567890ABCD12345678", default) };
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data.OldCount);
        }

        // ── Master password age ─────────────────────────────────────────

        [Fact]
        public async Task Check_MasterPasswordOlderThan90Days_ReturnsMasterOldTrue()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow.AddDays(-100)));
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.True(result.Data.MasterPasswordOld);
        }

        [Fact]
        public async Task Check_RecentMasterPassword_ReturnsMasterOldFalse()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow.AddDays(-10)));
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.False(result.Data.MasterPasswordOld);
        }

        [Fact]
        public async Task Check_UserNotFound_MasterOldFalse()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync((User)null);
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.False(result.Data.MasterPasswordOld);
        }

        // ── Combined scenario ───────────────────────────────────────────

        [Fact]
        public async Task Check_MixedVault_ReturnsCorrectCounts()
        {
            var (vaultRepo, userRepo, hibp) = CreateMocks();
            var sha1Breached = "5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8";
            var sha1Safe     = "ABCD1234567890ABCD1234567890ABCD12345678";
            var items = new List<VaultItem>
            {
                MakeItem(sha1Breached, DateTime.UtcNow.AddDays(-100)), // breached AND old
                MakeItem(sha1Safe, DateTime.UtcNow.AddDays(-200)),     // old only
                MakeItem(sha1Safe, DateTime.UtcNow.AddDays(-5)),       // neither
            };
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow.AddDays(-91)));
            hibp.Setup(h => h.IsPasswordPwnedAsync(sha1Breached)).ReturnsAsync(true);
            hibp.Setup(h => h.IsPasswordPwnedAsync(sha1Safe)).ReturnsAsync(false);
            var manager = new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

            var result = await manager.CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data.BreachedCount);
            Assert.Equal(2, result.Data.OldCount);
            Assert.True(result.Data.MasterPasswordOld);
        }
    }
}
