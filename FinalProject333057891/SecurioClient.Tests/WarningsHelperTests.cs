using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurioClient.Helpers;
using SecurioModels.DataTransferObjects;
using Xunit;

namespace SecurioClient.Tests
{
    // Comprehensive unit tests for WarningsHelper, covering:
    //   1. ComputeWarningsSync — all four risk counters (leaked, weak, reused, old)
    //   2. GetItemsAtRiskAsync — all four categories
    //   3. Counter-to-list consistency — the displayed number on the Warnings page
    //      must equal the count of items returned when the user taps "View All"
    //
    // The AES-GCM encryption (needed for the "weak" counter) is provided by the
    // test-stub in Stubs/EncryptionHelper.cs so no Android SDK is required.
    // The HIBP network call is provided by the test-stub in Stubs/HibpClientService.cs
    // which returns true only for hashes explicitly added to PwnedHashes.
    //
    // To run: dotnet test SecurioClient.Tests/SecurioClient.Tests.csproj
    public class WarningsHelperTests : IDisposable
    {
        // A 32-byte all-zeros AES key — valid for AES-256 and deterministic for tests.
        private static readonly string TestVaultKey = Convert.ToBase64String(new byte[32]);

        // A valid 40-char hex SHA-1 hash that the stub will treat as "clean".
        private const string SafeHash1 = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        private const string SafeHash2 = "AABBCCDDEEFF00112233445566778899AABBCC22";
        private const string SafeHash3 = "AABBCCDDEEFF00112233445566778899AABBCC33";

        // Hashes that tests can register as "pwned" in the HIBP stub.
        private const string PwnedHash1 = "DEADBEEF1234567890ABCDEF1234567890AABBCC";
        private const string PwnedHash2 = "DEADBEEF1234567890ABCDEF1234567890AABBDD";

        public WarningsHelperTests()
        {
            // Start each test with a clean HIBP stub and no pre-registered hashes.
            HibpClientService.Reset();
        }

        public void Dispose()
        {
            HibpClientService.Reset();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        // Creates a VaultItem whose password is encrypted with TestVaultKey so that
        // WarningsHelper.GetWeakItems can decrypt it for strength analysis.
        private static VaultItem EncryptedItem(
            string password,
            string sha1Hash    = SafeHash1,
            bool   isLeaked    = false,
            DateTime lastUpdate = default)
        {
            var (iv, tag, ct) = SecurioClient.EncryptionHelper.EncryptAesGcm(password, TestVaultKey);
            return new VaultItem
            {
                AccountName  = "TestAccount",
                Sha1Hash     = sha1Hash,
                IsLeaked     = isLeaked,
                LastUpdate   = lastUpdate == default ? DateTime.UtcNow : lastUpdate,
                IV           = iv,
                Tag          = tag,
                CipherText   = ct
            };
        }

        // ── ComputeWarningsSync: null / empty vault ───────────────────────────────

        [Fact]
        public void ComputeWarningsSync_NullVault_ReturnsAllZeros()
        {
            var result = WarningsHelper.ComputeWarningsSync(null!, TestVaultKey);
            Assert.Equal(0, result.LeakedCount);
            Assert.Equal(0, result.WeakCount);
            Assert.Equal(0, result.ReusedCount);
            Assert.Equal(0, result.OldCount);
        }

        [Fact]
        public void ComputeWarningsSync_EmptyVault_ReturnsAllZeros()
        {
            var result = WarningsHelper.ComputeWarningsSync(new List<VaultItem>(), TestVaultKey);
            Assert.Equal(0, result.LeakedCount);
            Assert.Equal(0, result.WeakCount);
            Assert.Equal(0, result.ReusedCount);
            Assert.Equal(0, result.OldCount);
        }

        // ── ComputeWarningsSync: LeakedCount ──────────────────────────────────────

        [Fact]
        public void ComputeWarningsSync_NoItemsLeaked_LeakedCountIsZero()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1, isLeaked: false),
                EncryptedItem("StrongP@ss2!", SafeHash2, isLeaked: false)
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(0, result.LeakedCount);
        }

        [Fact]
        public void ComputeWarningsSync_OneItemLeaked_LeakedCountIsOne()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1, isLeaked: true),
                EncryptedItem("StrongP@ss2!", SafeHash2, isLeaked: false)
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(1, result.LeakedCount);
        }

        [Fact]
        public void ComputeWarningsSync_AllItemsLeaked_LeakedCountMatchesVaultSize()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", PwnedHash1, isLeaked: true),
                EncryptedItem("StrongP@ss2!", PwnedHash2, isLeaked: true)
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(2, result.LeakedCount);
        }

        // ── ComputeWarningsSync: WeakCount ────────────────────────────────────────

        [Fact]
        public void ComputeWarningsSync_StrongPassword_WeakCountIsZero()
        {
            // "StrongP@ss1!" passes all five ValidationHelper criteria.
            var vault  = new List<VaultItem> { EncryptedItem("StrongP@ss1!") };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(0, result.WeakCount);
        }

        [Fact]
        public void ComputeWarningsSync_WeakPassword_WeakCountIsOne()
        {
            // "weakpass" has length >= 8 but no uppercase, no digit, no special → weak.
            var vault  = new List<VaultItem> { EncryptedItem("weakpass") };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(1, result.WeakCount);
        }

        [Fact]
        public void ComputeWarningsSync_TwoWeakOneStrong_WeakCountIsTwo()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("weakpass",   SafeHash1),
                EncryptedItem("alsoweakk",  SafeHash2),
                EncryptedItem("StrongP@ss1!", SafeHash3)
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(2, result.WeakCount);
        }

        [Fact]
        public void ComputeWarningsSync_ItemWithMissingCryptoFields_SkippedForWeak()
        {
            // Items with missing IV/Tag/CipherText must be skipped, not crash.
            var item = new VaultItem
            {
                AccountName = "NoPassword",
                Sha1Hash    = SafeHash1,
                IV          = null!,
                Tag         = null!,
                CipherText  = null!,
                LastUpdate  = DateTime.UtcNow
            };
            var vault  = new List<VaultItem> { item };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(0, result.WeakCount);
        }

        [Fact]
        public void ComputeWarningsSync_ItemWithEmptyCryptoFields_SkippedForWeak()
        {
            var item = new VaultItem
            {
                AccountName = "Empty",
                Sha1Hash    = SafeHash1,
                IV          = "",
                Tag         = "",
                CipherText  = "",
                LastUpdate  = DateTime.UtcNow
            };
            var vault  = new List<VaultItem> { item };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(0, result.WeakCount);
        }

        // Password boundary checks for the "weak" classifier
        [Fact]
        public void ComputeWarningsSync_PasswordMissingUppercase_CountedAsWeak()
        {
            var vault  = new List<VaultItem> { EncryptedItem("nouppercase1!") };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(1, result.WeakCount);
        }

        [Fact]
        public void ComputeWarningsSync_PasswordMissingDigit_CountedAsWeak()
        {
            var vault  = new List<VaultItem> { EncryptedItem("NoDigitHere!") };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(1, result.WeakCount);
        }

        [Fact]
        public void ComputeWarningsSync_PasswordMissingSpecial_CountedAsWeak()
        {
            var vault  = new List<VaultItem> { EncryptedItem("NoSpecial1a") };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(1, result.WeakCount);
        }

        // ── ComputeWarningsSync: ReusedCount ──────────────────────────────────────

        [Fact]
        public void ComputeWarningsSync_AllUniqueHashes_ReusedCountIsZero()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),
                EncryptedItem("StrongP@ss2!", SafeHash2)
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(0, result.ReusedCount);
        }

        [Fact]
        public void ComputeWarningsSync_TwoItemsShareHash_ReusedCountIsTwo()
        {
            // Both items in the pair are counted, not just one.
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),
                EncryptedItem("StrongP@ss2!", SafeHash1)   // same hash as above
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(2, result.ReusedCount);
        }

        [Fact]
        public void ComputeWarningsSync_ThreeItemsShareHash_ReusedCountIsThree()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),
                EncryptedItem("StrongP@ss2!", SafeHash1),
                EncryptedItem("StrongP@ss3!", SafeHash1)
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(3, result.ReusedCount);
        }

        [Fact]
        public void ComputeWarningsSync_TwoPairsSharedHash_ReusedCountIsFour()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),
                EncryptedItem("StrongP@ss2!", SafeHash1),
                EncryptedItem("StrongP@ss3!", SafeHash2),
                EncryptedItem("StrongP@ss4!", SafeHash2)
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(4, result.ReusedCount);
        }

        [Fact]
        public void ComputeWarningsSync_ItemWithNullHash_NotCountedAsReused()
        {
            // Null hashes must be excluded from the reuse grouping.
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),
                new VaultItem { Sha1Hash = null!, LastUpdate = DateTime.UtcNow }
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(0, result.ReusedCount);
        }

        // ── ComputeWarningsSync: OldCount ─────────────────────────────────────────

        [Fact]
        public void ComputeWarningsSync_FreshPassword_OldCountIsZero()
        {
            var vault  = new List<VaultItem> { EncryptedItem("StrongP@ss1!", lastUpdate: DateTime.UtcNow) };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(0, result.OldCount);
        }

        [Fact]
        public void ComputeWarningsSync_PasswordOver90DaysOld_OldCountIsOne()
        {
            var vault  = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", lastUpdate: DateTime.UtcNow.AddDays(-91))
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(1, result.OldCount);
        }

        [Fact]
        public void ComputeWarningsSync_PasswordUnder90DaysOld_NotFlagged()
        {
            // 89 days old is clearly below the 90-day threshold.
            var vault  = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", lastUpdate: DateTime.UtcNow.AddDays(-89))
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(0, result.OldCount);
        }

        [Fact]
        public void ComputeWarningsSync_PasswordJustOver90Days_Flagged()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", lastUpdate: DateTime.UtcNow.AddDays(-91))
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(1, result.OldCount);
        }

        [Fact]
        public void ComputeWarningsSync_DefaultLastUpdate_NotCountedAsOld()
        {
            // Items with DateTime.MinValue (never set) must be skipped to avoid false positives.
            var item = new VaultItem
            {
                AccountName = "New",
                Sha1Hash    = SafeHash1,
                LastUpdate  = default    // DateTime.MinValue
            };
            var vault  = new List<VaultItem> { item };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(0, result.OldCount);
        }

        [Fact]
        public void ComputeWarningsSync_MixOldAndFresh_OnlyOldCounted()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", lastUpdate: DateTime.UtcNow.AddDays(-120)),  // old
                EncryptedItem("StrongP@ss2!", SafeHash2, lastUpdate: DateTime.UtcNow)       // fresh
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            Assert.Equal(1, result.OldCount);
        }

        // ── GetItemsAtRiskAsync: "leaked" category ────────────────────────────────

        [Fact]
        public async Task GetItemsAtRisk_Leaked_EmptyVault_ReturnsEmptyList()
        {
            var result = await WarningsHelper.GetItemsAtRiskAsync(
                new List<VaultItem>(), TestVaultKey, "leaked");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetItemsAtRisk_Leaked_NoPwnedHashes_ReturnsEmptyList()
        {
            // HIBP stub returns false for everything by default.
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),
                EncryptedItem("StrongP@ss2!", SafeHash2)
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "leaked");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetItemsAtRisk_Leaked_OneLeakedItem_ReturnsOneItem()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", PwnedHash1, isLeaked: true),
                EncryptedItem("StrongP@ss2!", SafeHash1,  isLeaked: false)
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "leaked");
            Assert.Single(result);
            Assert.Equal(PwnedHash1, result[0].Sha1Hash);
        }

        [Fact]
        public async Task GetItemsAtRisk_Leaked_ItemWithIsLeakedFalse_NotReturned()
        {
            var vault = new List<VaultItem>
            {
                new VaultItem { Sha1Hash = SafeHash1, IsLeaked = false, LastUpdate = DateTime.UtcNow }
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "leaked");
            Assert.Empty(result);
        }

        // ── GetItemsAtRiskAsync: "weak" category ──────────────────────────────────

        [Fact]
        public async Task GetItemsAtRisk_Weak_AllStrongPasswords_ReturnsEmptyList()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),
                EncryptedItem("StrongP@ss2!", SafeHash2)
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "weak");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetItemsAtRisk_Weak_OneWeakPassword_ReturnsOneItem()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("weakpass",      SafeHash1),
                EncryptedItem("StrongP@ss1!",  SafeHash2)
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "weak");
            Assert.Single(result);
            Assert.Equal(SafeHash1, result[0].Sha1Hash);
        }

        [Fact]
        public async Task GetItemsAtRisk_Weak_MissingCryptoFields_SkippedSafely()
        {
            var item = new VaultItem { Sha1Hash = SafeHash1, LastUpdate = DateTime.UtcNow };
            var result = await WarningsHelper.GetItemsAtRiskAsync(
                new List<VaultItem> { item }, TestVaultKey, "weak");
            Assert.Empty(result);
        }

        // ── GetItemsAtRiskAsync: "reused" category ────────────────────────────────

        [Fact]
        public async Task GetItemsAtRisk_Reused_UniqueHashes_ReturnsEmptyList()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),
                EncryptedItem("StrongP@ss2!", SafeHash2)
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "reused");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetItemsAtRisk_Reused_TwoItemsShareHash_ReturnsBoth()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),
                EncryptedItem("StrongP@ss2!", SafeHash1)  // same hash
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "reused");
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetItemsAtRisk_Reused_OnlySharedGroupReturned_UniquesExcluded()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),  // unique
                EncryptedItem("StrongP@ss2!", SafeHash2),  // reused
                EncryptedItem("StrongP@ss3!", SafeHash2)   // reused
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "reused");
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Equal(SafeHash2, item.Sha1Hash));
        }

        // ── GetItemsAtRiskAsync: "old" category ───────────────────────────────────

        [Fact]
        public async Task GetItemsAtRisk_Old_FreshVault_ReturnsEmptyList()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", lastUpdate: DateTime.UtcNow)
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "old");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetItemsAtRisk_Old_StalePassword_ReturnsItem()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", lastUpdate: DateTime.UtcNow.AddDays(-100))
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "old");
            Assert.Single(result);
        }

        [Fact]
        public async Task GetItemsAtRisk_Old_DefaultDate_NotReturned()
        {
            var item = new VaultItem { Sha1Hash = SafeHash1, LastUpdate = default };
            var result = await WarningsHelper.GetItemsAtRiskAsync(
                new List<VaultItem> { item }, TestVaultKey, "old");
            Assert.Empty(result);
        }

        // ── GetItemsAtRiskAsync: unknown category ─────────────────────────────────

        [Fact]
        public async Task GetItemsAtRisk_UnknownCategory_ReturnsEmptyList()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!")
            };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "invalid");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetItemsAtRisk_NullCategory_ReturnsEmptyList()
        {
            var vault = new List<VaultItem> { EncryptedItem("StrongP@ss1!") };
            var result = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, null!);
            Assert.Empty(result);
        }

        // ── Counter-to-list consistency ───────────────────────────────────────────
        //
        // This is the most critical set of tests — they verify that the numbers shown
        // on the WarningsActivity ("Leaked: 2  Weak: 1  Reused: 4  Old: 1") exactly
        // match the item counts returned when the user taps "View All" for each category.
        // Mismatches here would mean the counter and the list show contradictory data.

        [Fact]
        public async Task CounterConsistency_LeakedCount_MatchesGetItemsAtRiskLeaked()
        {
            // Register exactly two hashes as "pwned" in the HIBP stub.
            HibpClientService.PwnedHashes.Add(PwnedHash1);
            HibpClientService.PwnedHashes.Add(PwnedHash2);

            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", PwnedHash1, isLeaked: true),
                EncryptedItem("StrongP@ss2!", PwnedHash2, isLeaked: true),
                EncryptedItem("StrongP@ss3!", SafeHash1,  isLeaked: false)
            };

            var counters = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            var items    = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "leaked");

            Assert.Equal(counters.LeakedCount, items.Count);
        }

        [Fact]
        public async Task CounterConsistency_WeakCount_MatchesGetItemsAtRiskWeak()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("weakpass",      SafeHash1),
                EncryptedItem("alsoweakk",     SafeHash2),
                EncryptedItem("StrongP@ss1!",  SafeHash3)
            };

            var counters = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            var items    = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "weak");

            Assert.Equal(counters.WeakCount, items.Count);
        }

        [Fact]
        public async Task CounterConsistency_ReusedCount_MatchesGetItemsAtRiskReused()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", SafeHash1),  // unique
                EncryptedItem("StrongP@ss2!", SafeHash2),  // reused pair
                EncryptedItem("StrongP@ss3!", SafeHash2)   // reused pair
            };

            var counters = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            var items    = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "reused");

            Assert.Equal(counters.ReusedCount, items.Count);
        }

        [Fact]
        public async Task CounterConsistency_OldCount_MatchesGetItemsAtRiskOld()
        {
            var vault = new List<VaultItem>
            {
                EncryptedItem("StrongP@ss1!", lastUpdate: DateTime.UtcNow.AddDays(-100)),  // old
                EncryptedItem("StrongP@ss2!", SafeHash2, lastUpdate: DateTime.UtcNow)       // fresh
            };

            var counters = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);
            var items    = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "old");

            Assert.Equal(counters.OldCount, items.Count);
        }

        [Fact]
        public async Task CounterConsistency_AllCategories_MixedVault_AllMatch()
        {
            // Build a vault that has at least one item in every category.
            HibpClientService.PwnedHashes.Add(PwnedHash1);

            var vault = new List<VaultItem>
            {
                // Leaked
                EncryptedItem("StrongP@ss1!", PwnedHash1, isLeaked: true),
                // Weak
                EncryptedItem("weakpass",     SafeHash1),
                // Reused (pair)
                EncryptedItem("StrongP@ss2!", SafeHash2),
                EncryptedItem("StrongP@ss3!", SafeHash2),
                // Old
                EncryptedItem("StrongP@ss4!", SafeHash3, lastUpdate: DateTime.UtcNow.AddDays(-200))
            };

            var counters = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);

            var leakedItems  = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "leaked");
            var weakItems    = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "weak");
            var reusedItems  = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "reused");
            var oldItems     = await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "old");

            Assert.Equal(counters.LeakedCount,  leakedItems.Count);
            Assert.Equal(counters.WeakCount,     weakItems.Count);
            Assert.Equal(counters.ReusedCount,   reusedItems.Count);
            Assert.Equal(counters.OldCount,      oldItems.Count);
        }

        [Fact]
        public async Task CounterConsistency_EmptyVault_AllCountersAndListsAreZero()
        {
            var vault    = new List<VaultItem>();
            var counters = WarningsHelper.ComputeWarningsSync(vault, TestVaultKey);

            Assert.Equal(0, counters.LeakedCount);
            Assert.Equal(0, counters.WeakCount);
            Assert.Equal(0, counters.ReusedCount);
            Assert.Equal(0, counters.OldCount);

            Assert.Empty(await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "leaked"));
            Assert.Empty(await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "weak"));
            Assert.Empty(await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "reused"));
            Assert.Empty(await WarningsHelper.GetItemsAtRiskAsync(vault, TestVaultKey, "old"));
        }
    }
}
