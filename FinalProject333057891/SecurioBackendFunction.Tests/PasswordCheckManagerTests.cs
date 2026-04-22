using Moq;
using Xunit;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Tests
{
    // Comprehensive unit tests for PasswordCheckManager.GetPasswordCheckAsync.
    // Covers: user not found, empty vault, breached passwords, old passwords,
    // old master password, and combinations thereof.
    // All repository dependencies are mocked.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class PasswordCheckManagerTests
    {
        private static PasswordCheckManager Build(
            Mock<IUserRepository>? userRepo  = null,
            Mock<IVaultItemRepository>? vault = null)
        {
            userRepo ??= new Mock<IUserRepository>();
            vault    ??= new Mock<IVaultItemRepository>();
            return new PasswordCheckManager(userRepo.Object, vault.Object);
        }

        private static User FreshUser() => new User
        {
            Id                  = 1,
            Username            = "alice",
            LastPasswordUpdate  = DateTime.UtcNow.AddDays(-10)  // changed 10 days ago — not old
        };

        private static User OldMasterPasswordUser() => new User
        {
            Id                  = 1,
            Username            = "alice",
            LastPasswordUpdate  = DateTime.UtcNow.AddDays(-100)  // 100 days ago — old
        };

        private static VaultItem FreshItem(bool leaked = false) => new VaultItem
        {
            Id         = 1,
            AccountName = "Gmail",
            IsLeaked   = leaked,
            LastUpdate = DateTime.UtcNow.AddDays(-5)   // 5 days ago — not old
        };

        private static VaultItem OldItem(bool leaked = false) => new VaultItem
        {
            Id         = 2,
            AccountName = "Twitter",
            IsLeaked   = leaked,
            LastUpdate = DateTime.UtcNow.AddDays(-100)  // 100 days ago — old
        };

        // ── User not found ────────────────────────────────────────────────────────

        [Fact]
        public async Task UserNotFound_ReturnsNull()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(99)).ReturnsAsync((User?)null);

            var result = await Build(repo).GetPasswordCheckAsync(99);

            Assert.Null(result);
        }

        // ── Empty vault ───────────────────────────────────────────────────────────

        [Fact]
        public async Task EmptyVault_FreshMasterPassword_AllCountersZero()
        {
            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.NotNull(result);
            Assert.Equal(0, result!.BreachedCount);
            Assert.Equal(0, result.OldCount);
            Assert.False(result.MasterPasswordOld);
        }

        // ── BreachedCount ─────────────────────────────────────────────────────────

        [Fact]
        public async Task OneLeakedItem_BreachedCountIs1()
        {
            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem> { FreshItem(leaked: true), FreshItem(leaked: false) });

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.Equal(1, result!.BreachedCount);
        }

        [Fact]
        public async Task ThreeLeakedItems_BreachedCountIs3()
        {
            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem>
                     {
                         FreshItem(leaked: true),
                         FreshItem(leaked: true),
                         FreshItem(leaked: true),
                         FreshItem(leaked: false)
                     });

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.Equal(3, result!.BreachedCount);
        }

        [Fact]
        public async Task NoLeakedItems_BreachedCountIs0()
        {
            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem> { FreshItem(false), FreshItem(false) });

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.Equal(0, result!.BreachedCount);
        }

        // ── OldCount ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task OneOldItem_OldCountIs1()
        {
            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem> { OldItem(), FreshItem() });

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.Equal(1, result!.OldCount);
        }

        [Fact]
        public async Task FreshItem_ExactlyAt89Days_NotCountedAsOld()
        {
            var item = new VaultItem
            {
                Id         = 3,
                LastUpdate = DateTime.UtcNow.AddDays(-89),
                IsLeaked   = false
            };

            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem> { item });

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.Equal(0, result!.OldCount);
        }

        [Fact]
        public async Task ItemAt91Days_CountedAsOld()
        {
            var item = new VaultItem
            {
                Id         = 4,
                LastUpdate = DateTime.UtcNow.AddDays(-91),
                IsLeaked   = false
            };

            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem> { item });

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.Equal(1, result!.OldCount);
        }

        // ── MasterPasswordOld ─────────────────────────────────────────────────────

        [Fact]
        public async Task FreshMasterPassword_MasterPasswordOldIsFalse()
        {
            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.False(result!.MasterPasswordOld);
        }

        [Fact]
        public async Task OldMasterPassword_MasterPasswordOldIsTrue()
        {
            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(OldMasterPasswordUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.True(result!.MasterPasswordOld);
        }

        [Fact]
        public async Task MasterPasswordAt89Days_NotOld()
        {
            var user = new User
            {
                Id                 = 1,
                LastPasswordUpdate = DateTime.UtcNow.AddDays(-89)
            };

            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(user);
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.False(result!.MasterPasswordOld);
        }

        [Fact]
        public async Task MasterPasswordAt91Days_IsOld()
        {
            var user = new User
            {
                Id                 = 1,
                LastPasswordUpdate = DateTime.UtcNow.AddDays(-91)
            };

            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(user);
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.True(result!.MasterPasswordOld);
        }

        // ── Combined ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task AllIssues_AllCountersPopulated()
        {
            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(OldMasterPasswordUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem>
                     {
                         FreshItem(leaked: true),
                         OldItem(leaked: false),
                         OldItem(leaked: true)  // both old AND leaked
                     });

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.Equal(2, result!.BreachedCount);   // 2 leaked items
            Assert.Equal(2, result.OldCount);          // 2 old items
            Assert.True(result.MasterPasswordOld);
        }

        [Fact]
        public async Task OnlyBreachedCount_OtherCountersZero()
        {
            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem>
                     {
                         FreshItem(leaked: true),
                         FreshItem(leaked: false)
                     });

            var result = await Build(userRepo, vaultRepo).GetPasswordCheckAsync(1);

            Assert.Equal(1, result!.BreachedCount);
            Assert.Equal(0, result.OldCount);
            Assert.False(result.MasterPasswordOld);
        }
    }
}
