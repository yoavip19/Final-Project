using Moq;
using Xunit;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Tests
{
    // Comprehensive unit tests for PasswordCheckManager.GetPasswordCheckAsync.
    // Covers: user not found, empty vault, HIBP re-check (new breach / cleared / no hash),
    // IsLeaked DB update, old passwords, old master password, and combinations thereof.
    // All repository and HIBP dependencies are mocked.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class PasswordCheckManagerTests
    {
        private const string SafeHash  = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        private const string PwnedHash = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        private static PasswordCheckManager Build(
            Mock<IUserRepository>? userRepo  = null,
            Mock<IVaultItemRepository>? vault = null,
            Mock<IHibpService>? hibp          = null)
        {
            userRepo ??= new Mock<IUserRepository>();
            vault    ??= new Mock<IVaultItemRepository>();
            if (hibp == null)
            {
                hibp = new Mock<IHibpService>();
                // Default: every hash is clean.
                hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);
            }
            // Default: UpdateIsLeakedAsync is a no-op.
            vault.Setup(v => v.UpdateIsLeakedAsync(It.IsAny<int>(), It.IsAny<bool>()))
                 .Returns(Task.CompletedTask);
            return new PasswordCheckManager(userRepo.Object, vault.Object, hibp.Object);
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

        private static VaultItem FreshItem(bool leaked = false, string sha1Hash = null) => new VaultItem
        {
            Id          = 1,
            AccountName = "Gmail",
            IsLeaked    = leaked,
            Sha1Hash    = sha1Hash,
            LastUpdate  = DateTime.UtcNow.AddDays(-5)   // 5 days ago — not old
        };

        private static VaultItem OldItem(bool leaked = false, string sha1Hash = null) => new VaultItem
        {
            Id          = 1,
            AccountName = "OldSite",
            IsLeaked    = leaked,
            Sha1Hash    = sha1Hash,
            LastUpdate  = DateTime.UtcNow.AddDays(-100)  // 100 days ago — old
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

        // ── HIBP re-check: new breach detected ───────────────────────────────────

        [Fact]
        public async Task ItemWasClean_HibpNowPwned_BreachedCountIs1()
        {
            // Item was stored as not-leaked (IsLeaked = false), but HIBP now says it is.
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem> { FreshItem(leaked: false, sha1Hash: PwnedHash) });

            var result = await Build(userRepo, vaultRepo, hibp).GetPasswordCheckAsync(1);

            Assert.Equal(1, result!.BreachedCount);
        }

        [Fact]
        public async Task ItemWasClean_HibpNowPwned_UpdateIsLeakedCalledWithTrue()
        {
            // When HIBP reports a new breach, the DB must be updated so the vault UI stays accurate.
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            var item = FreshItem(leaked: false, sha1Hash: PwnedHash);
            item.Id = 42;
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem> { item });
            vaultRepo.Setup(v => v.UpdateIsLeakedAsync(It.IsAny<int>(), It.IsAny<bool>()))
                     .Returns(Task.CompletedTask);

            await Build(userRepo, vaultRepo, hibp).GetPasswordCheckAsync(1);

            vaultRepo.Verify(v => v.UpdateIsLeakedAsync(42, true), Times.Once);
        }

        // ── HIBP re-check: breach cleared ─────────────────────────────────────────

        [Fact]
        public async Task ItemWasLeaked_HibpNowClean_BreachedCountIs0()
        {
            // Edge case: item was previously leaked but HIBP no longer reports it (e.g. false positive removed).
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem> { FreshItem(leaked: true, sha1Hash: SafeHash) });

            var result = await Build(userRepo, vaultRepo, hibp).GetPasswordCheckAsync(1);

            Assert.Equal(0, result!.BreachedCount);
        }

        [Fact]
        public async Task ItemWasLeaked_HibpNowClean_UpdateIsLeakedCalledWithFalse()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            var item = FreshItem(leaked: true, sha1Hash: SafeHash);
            item.Id = 7;
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem> { item });
            vaultRepo.Setup(v => v.UpdateIsLeakedAsync(It.IsAny<int>(), It.IsAny<bool>()))
                     .Returns(Task.CompletedTask);

            await Build(userRepo, vaultRepo, hibp).GetPasswordCheckAsync(1);

            vaultRepo.Verify(v => v.UpdateIsLeakedAsync(7, false), Times.Once);
        }

        // ── HIBP re-check: status unchanged → no DB write ─────────────────────────

        [Fact]
        public async Task ItemStatusUnchanged_UpdateIsLeakedNotCalled()
        {
            // If HIBP agrees with the cached flag, there is no DB write.
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem> { FreshItem(leaked: true, sha1Hash: PwnedHash) });
            vaultRepo.Setup(v => v.UpdateIsLeakedAsync(It.IsAny<int>(), It.IsAny<bool>()))
                     .Returns(Task.CompletedTask);

            await Build(userRepo, vaultRepo, hibp).GetPasswordCheckAsync(1);

            vaultRepo.Verify(v => v.UpdateIsLeakedAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        // ── HIBP re-check: no Sha1Hash stored → skip HIBP, trust cached flag ─────

        [Fact]
        public async Task ItemWithNoSha1Hash_HibpNotCalled_CachedFlagUsed()
        {
            // Items without a stored hash cannot be re-checked; the cached IsLeaked value is used as-is.
            var hibp = new Mock<IHibpService>();

            var userRepo  = new Mock<IUserRepository>();
            var vaultRepo = new Mock<IVaultItemRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                     .ReturnsAsync(new List<VaultItem> { FreshItem(leaked: true, sha1Hash: null) });

            var result = await Build(userRepo, vaultRepo, hibp).GetPasswordCheckAsync(1);

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
            Assert.Equal(1, result!.BreachedCount);  // cached value preserved
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
