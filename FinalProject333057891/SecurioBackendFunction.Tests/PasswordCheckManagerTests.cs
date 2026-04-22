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
    // Unit tests for PasswordCheckManager.CheckAsync.
    // Covers: validation, empty vault, breached passwords, old passwords,
    // master password age, mixed scenarios, and edge cases.
    public class PasswordCheckManagerTests
    {
        private static VaultItem MakeItem(string sha1, DateTime lastUpdate, bool isLeaked = false) =>
            new VaultItem
            {
                Id = 1,
                UserId = 1,
                AccountName = "TestSite",
                Sha1Hash = sha1,
                LastUpdate = lastUpdate,
                IsLeaked = isLeaked
            };

        private static User MakeUser(DateTime lastPasswordUpdate) => new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            LastPasswordUpdate = lastPasswordUpdate
        };

        private static PasswordCheckManager Build(
            Mock<IVaultItemRepository> vaultRepo,
            Mock<IUserRepository> userRepo,
            Mock<IHibpService> hibp)
            => new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object);

        // ── Validation ───────────────────────────────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task Check_InvalidUserId_ReturnsFail(int userId)
        {
            var result = await Build(
                new Mock<IVaultItemRepository>(),
                new Mock<IUserRepository>(),
                new Mock<IHibpService>()).CheckAsync(userId);

            Assert.False(result.Success);
            Assert.Equal("Invalid user ID.", result.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Check_InvalidUserId_NoRepositoryCalls(int userId)
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            var userRepo  = new Mock<IUserRepository>();
            var hibp      = new Mock<IHibpService>();

            await Build(vaultRepo, userRepo, hibp).CheckAsync(userId);

            vaultRepo.Verify(r => r.GetVaultItemsByUserIdAsync(It.IsAny<int>()), Times.Never);
            userRepo.Verify(r => r.GetUserProfileAsync(It.IsAny<int>()), Times.Never);
        }

        // ── Empty vault ──────────────────────────────────────────────────────────

        [Fact]
        public async Task Check_EmptyVault_ReturnsAllZeroCounts()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));

            var result = await Build(vaultRepo, userRepo, new Mock<IHibpService>()).CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.BreachedCount);
            Assert.Equal(0, result.Data.OldCount);
            Assert.False(result.Data.MasterPasswordOld);
        }

        [Fact]
        public async Task Check_EmptyVault_HibpNeverCalled()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var hibp = new Mock<IHibpService>();

            await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        // ── Breached password counting ────────────────────────────────────────────

        [Fact]
        public async Task Check_OneBreachedPassword_BreachedCountIsOne()
        {
            const string sha1 = "5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8";
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                .ReturnsAsync(new List<VaultItem> { MakeItem(sha1, DateTime.UtcNow) });
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(sha1)).ReturnsAsync(true);

            var result = await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            Assert.Equal(1, result.Data!.BreachedCount);
        }

        [Fact]
        public async Task Check_ThreeBreachedTwoSafe_BreachedCountIsThree()
        {
            const string pwnedSha1 = "5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8";
            const string safeSha1  = "ABCD1234567890ABCD1234567890ABCD12345678";
            var items = new List<VaultItem>
            {
                MakeItem(pwnedSha1, DateTime.UtcNow),
                MakeItem(pwnedSha1, DateTime.UtcNow),
                MakeItem(pwnedSha1, DateTime.UtcNow),
                MakeItem(safeSha1, DateTime.UtcNow),
                MakeItem(safeSha1, DateTime.UtcNow)
            };
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(pwnedSha1)).ReturnsAsync(true);
            hibp.Setup(h => h.IsPasswordPwnedAsync(safeSha1)).ReturnsAsync(false);

            var result = await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            Assert.Equal(3, result.Data!.BreachedCount);
        }

        [Fact]
        public async Task Check_ItemWithEmptySha1_SkippedByHibp()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                .ReturnsAsync(new List<VaultItem> { MakeItem("", DateTime.UtcNow) });
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var hibp = new Mock<IHibpService>();

            await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Check_NullSha1_SkippedByHibp()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            var item = MakeItem("", DateTime.UtcNow);
            item.Sha1Hash = null;
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                .ReturnsAsync(new List<VaultItem> { item });
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var hibp = new Mock<IHibpService>();

            await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        // ── Old password counting ─────────────────────────────────────────────────

        [Fact]
        public async Task Check_PasswordOlderThan90Days_OldCountIsOne()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                .ReturnsAsync(new List<VaultItem>
                {
                    MakeItem("ABCD1234567890ABCD1234567890ABCD12345678", DateTime.UtcNow.AddDays(-100))
                });
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);

            var result = await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            Assert.Equal(1, result.Data!.OldCount);
        }

        [Fact]
        public async Task Check_PasswordExactlyAt89Days_NotCountedAsOld()
        {
            // 89 days is below the 90-day threshold and must not be flagged.
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                .ReturnsAsync(new List<VaultItem>
                {
                    MakeItem("ABCD1234567890ABCD1234567890ABCD12345678", DateTime.UtcNow.AddDays(-89))
                });
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);

            var result = await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            Assert.Equal(0, result.Data!.OldCount);
        }

        [Fact]
        public async Task Check_RecentPassword_OldCountIsZero()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                .ReturnsAsync(new List<VaultItem>
                {
                    MakeItem("ABCD1234567890ABCD1234567890ABCD12345678", DateTime.UtcNow.AddDays(-10))
                });
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);

            var result = await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            Assert.Equal(0, result.Data!.OldCount);
        }

        [Fact]
        public async Task Check_ItemWithDefaultLastUpdate_NotCountedAsOld()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                .ReturnsAsync(new List<VaultItem>
                {
                    MakeItem("ABCD1234567890ABCD1234567890ABCD12345678", default)
                });
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);

            var result = await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            Assert.Equal(0, result.Data!.OldCount);
        }

        // ── Master password age ───────────────────────────────────────────────────

        [Fact]
        public async Task Check_MasterPasswordOlderThan90Days_MasterOldIsTrue()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1))
                .ReturnsAsync(MakeUser(DateTime.UtcNow.AddDays(-100)));

            var result = await Build(vaultRepo, userRepo, new Mock<IHibpService>()).CheckAsync(1);

            Assert.True(result.Data!.MasterPasswordOld);
        }

        [Fact]
        public async Task Check_RecentMasterPassword_MasterOldIsFalse()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1))
                .ReturnsAsync(MakeUser(DateTime.UtcNow.AddDays(-10)));

            var result = await Build(vaultRepo, userRepo, new Mock<IHibpService>()).CheckAsync(1);

            Assert.False(result.Data!.MasterPasswordOld);
        }

        [Fact]
        public async Task Check_UserNotFound_MasterOldIsFalse()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync((User)null);

            var result = await Build(vaultRepo, userRepo, new Mock<IHibpService>()).CheckAsync(1);

            Assert.True(result.Success);
            Assert.False(result.Data!.MasterPasswordOld);
        }

        [Fact]
        public async Task Check_UserWithDefaultLastPasswordUpdate_MasterOldIsFalse()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1))
                .ReturnsAsync(MakeUser(default));

            var result = await Build(vaultRepo, userRepo, new Mock<IHibpService>()).CheckAsync(1);

            Assert.False(result.Data!.MasterPasswordOld);
        }

        // ── Combined scenarios ────────────────────────────────────────────────────

        [Fact]
        public async Task Check_MixedVault_AllCountersCorrect()
        {
            const string pwnedSha1 = "5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8";
            const string safeSha1  = "ABCD1234567890ABCD1234567890ABCD12345678";

            var items = new List<VaultItem>
            {
                MakeItem(pwnedSha1, DateTime.UtcNow.AddDays(-100)), // breached AND old
                MakeItem(safeSha1,  DateTime.UtcNow.AddDays(-200)), // old only
                MakeItem(safeSha1,  DateTime.UtcNow.AddDays(-5)),   // neither
            };

            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1))
                .ReturnsAsync(MakeUser(DateTime.UtcNow.AddDays(-91)));
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(pwnedSha1)).ReturnsAsync(true);
            hibp.Setup(h => h.IsPasswordPwnedAsync(safeSha1)).ReturnsAsync(false);

            var result = await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data!.BreachedCount);
            Assert.Equal(2, result.Data.OldCount);
            Assert.True(result.Data.MasterPasswordOld);
        }

        [Fact]
        public async Task Check_AllSafe_AllCountersAreZeroAndMasterOldFalse()
        {
            const string safeSha1 = "ABCD1234567890ABCD1234567890ABCD12345678";
            var items = new List<VaultItem>
            {
                MakeItem(safeSha1, DateTime.UtcNow.AddDays(-5)),
                MakeItem(safeSha1, DateTime.UtcNow.AddDays(-10))
            };
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(safeSha1)).ReturnsAsync(false);

            var result = await Build(vaultRepo, userRepo, hibp).CheckAsync(1);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.BreachedCount);
            Assert.Equal(0, result.Data.OldCount);
            Assert.False(result.Data.MasterPasswordOld);
        }

        [Fact]
        public async Task Check_ValidUserId_MessageIsPwdCheckCompleted()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(MakeUser(DateTime.UtcNow));

            var result = await Build(vaultRepo, userRepo, new Mock<IHibpService>()).CheckAsync(1);

            Assert.Equal("Password check completed.", result.Message);
        }
    }
}
