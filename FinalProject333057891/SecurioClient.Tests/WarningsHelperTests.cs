using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using SecurioClient.Helpers;
using SecurioModels.DataTransferObjects;
using Xunit;

namespace SecurioClient.Tests
{
    // Unit tests for WarningsHelper — the client-side password-risk analyser.
    // ComputeWarningsSync is tested fully (no network).
    // ComputeWarningsAsync and GetItemsAtRiskAsync("leaked") are skipped because
    // they depend on a live HIBP network call that cannot be injected here.
    // All other risk categories ("weak", "reused", "old") are tested via
    // GetItemsAtRiskAsync which delegates to the synchronous helpers.
    public class WarningsHelperTests
    {
        // Derive a stable vault key and encrypt a plaintext password with it.
        private static string VaultKey { get; } = MakeKey();
        private static string MakeKey()
        {
            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);
            return Convert.ToBase64String(key);
        }

        private static VaultItem MakeEncryptedItem(string plaintext, DateTime? lastUpdate = null)
        {
            var (iv, tag, ct) = EncryptionHelper.EncryptAesGcm(plaintext, VaultKey);
            var sha1 = EncryptionHelper.ComputeSha1Hash(plaintext);
            return new VaultItem
            {
                Id          = Random.Shared.Next(1, 10000),
                IV          = iv,
                Tag         = tag,
                CipherText  = ct,
                Sha1Hash    = sha1,
                LastUpdate  = lastUpdate ?? DateTime.UtcNow,
                IsLeaked    = false
            };
        }

        // ── ComputeWarningsSync: empty / null vault ───────────────────────────────

        [Fact]
        public void ComputeWarningsSync_NullVault_ReturnsAllZero()
        {
            var result = WarningsHelper.ComputeWarningsSync(null, VaultKey);
            Assert.Equal(0, result.LeakedCount);
            Assert.Equal(0, result.WeakCount);
            Assert.Equal(0, result.ReusedCount);
            Assert.Equal(0, result.OldCount);
        }

        [Fact]
        public void ComputeWarningsSync_EmptyVault_ReturnsAllZero()
        {
            var result = WarningsHelper.ComputeWarningsSync(new List<VaultItem>(), VaultKey);
            Assert.Equal(0, result.LeakedCount);
            Assert.Equal(0, result.WeakCount);
            Assert.Equal(0, result.ReusedCount);
            Assert.Equal(0, result.OldCount);
        }

        // ── ComputeWarningsSync: leaked counter ────────────────────────────────────

        [Fact]
        public void ComputeWarningsSync_OneLeakedItem_LeakedCountIsOne()
        {
            var item = MakeEncryptedItem("Str0ng!Passw0rd#");
            item.IsLeaked = true;
            var result = WarningsHelper.ComputeWarningsSync(new[] { item }, VaultKey);
            Assert.Equal(1, result.LeakedCount);
        }

        [Fact]
        public void ComputeWarningsSync_NoLeakedItems_LeakedCountIsZero()
        {
            var item = MakeEncryptedItem("Str0ng!Passw0rd#");
            item.IsLeaked = false;
            var result = WarningsHelper.ComputeWarningsSync(new[] { item }, VaultKey);
            Assert.Equal(0, result.LeakedCount);
        }

        [Fact]
        public void ComputeWarningsSync_ThreeLeakedTwoNot_LeakedCountIsThree()
        {
            var leaked1 = MakeEncryptedItem("Abcdefg1!"); item_leaked(leaked1);
            var leaked2 = MakeEncryptedItem("Str0ng!P@ss"); item_leaked(leaked2);
            var leaked3 = MakeEncryptedItem("Another!P@ss3"); item_leaked(leaked3);
            var safe1   = MakeEncryptedItem("X9#aaaaaaaaa");
            var safe2   = MakeEncryptedItem("Str0ng#2pass!");

            var vault = new[] { leaked1, leaked2, leaked3, safe1, safe2 };
            var result = WarningsHelper.ComputeWarningsSync(vault, VaultKey);
            Assert.Equal(3, result.LeakedCount);

            static void item_leaked(VaultItem v) => v.IsLeaked = true;
        }

        // ── ComputeWarningsSync: weak counter ──────────────────────────────────────

        [Fact]
        public void ComputeWarningsSync_WeakPassword_WeakCountIsOne()
        {
            // "abcdefgh" fails uppercase, digit, and special-char checks.
            var vault = new[] { MakeEncryptedItem("abcdefgh") };
            var result = WarningsHelper.ComputeWarningsSync(vault, VaultKey);
            Assert.Equal(1, result.WeakCount);
        }

        [Fact]
        public void ComputeWarningsSync_StrongPassword_WeakCountIsZero()
        {
            var vault = new[] { MakeEncryptedItem("Str0ng!P@ssw0rd#") };
            var result = WarningsHelper.ComputeWarningsSync(vault, VaultKey);
            Assert.Equal(0, result.WeakCount);
        }

        [Fact]
        public void ComputeWarningsSync_TwoWeakTwoStrong_WeakCountIsTwo()
        {
            var vault = new VaultItem[]
            {
                MakeEncryptedItem("abcdefgh"),         // weak
                MakeEncryptedItem("ABCDEFGH"),         // weak (no lower, digit, special)
                MakeEncryptedItem("Str0ng!P@ss#1"),    // strong
                MakeEncryptedItem("Abcdefg1!pass#2")   // strong
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, VaultKey);
            Assert.Equal(2, result.WeakCount);
        }

        [Fact]
        public void ComputeWarningsSync_ItemMissingIV_SkippedForWeakCheck()
        {
            // Items without crypto fields should not crash.
            var item = MakeEncryptedItem("abcdefgh");
            item.IV = null;
            var result = WarningsHelper.ComputeWarningsSync(new[] { item }, VaultKey);
            Assert.Equal(0, result.WeakCount); // skipped — not counted
        }

        [Fact]
        public void ComputeWarningsSync_WrongKeyForDecryption_SkipsItem()
        {
            var item = MakeEncryptedItem("Weakpass1"); // encrypted with VaultKey
            byte[] wrongKeyBytes = new byte[32];
            RandomNumberGenerator.Fill(wrongKeyBytes);
            string wrongKey = Convert.ToBase64String(wrongKeyBytes);

            // Using wrong key — DecryptAesGcm throws, item is skipped.
            var result = WarningsHelper.ComputeWarningsSync(new[] { item }, wrongKey);
            Assert.Equal(0, result.WeakCount);
        }

        // ── ComputeWarningsSync: reused counter ────────────────────────────────────

        [Fact]
        public void ComputeWarningsSync_TwoItemsSameHash_ReusedCountIsTwo()
        {
            const string samePassword = "Str0ng!P@ssw0rd#";
            string sha1 = EncryptionHelper.ComputeSha1Hash(samePassword);
            var item1 = MakeEncryptedItem(samePassword);
            var item2 = MakeEncryptedItem(samePassword);
            // Ensure both have the same Sha1Hash.
            item1.Sha1Hash = sha1;
            item2.Sha1Hash = sha1;

            var result = WarningsHelper.ComputeWarningsSync(new[] { item1, item2 }, VaultKey);
            Assert.Equal(2, result.ReusedCount);
        }

        [Fact]
        public void ComputeWarningsSync_ThreeItemsSameHash_ReusedCountIsThree()
        {
            const string sha1 = "AABBCCDDEEFF00112233445566778899AABBCCDD";
            var vault = new[]
            {
                new VaultItem { Sha1Hash = sha1 },
                new VaultItem { Sha1Hash = sha1 },
                new VaultItem { Sha1Hash = sha1 }
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, VaultKey);
            Assert.Equal(3, result.ReusedCount);
        }

        [Fact]
        public void ComputeWarningsSync_AllUniqueHashes_ReusedCountIsZero()
        {
            var vault = new[]
            {
                new VaultItem { Sha1Hash = "AAAA" + new string('A', 36) },
                new VaultItem { Sha1Hash = "BBBB" + new string('B', 36) },
                new VaultItem { Sha1Hash = "CCCC" + new string('C', 36) }
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, VaultKey);
            Assert.Equal(0, result.ReusedCount);
        }

        [Fact]
        public void ComputeWarningsSync_ItemWithNullHash_IgnoredForReusedCheck()
        {
            var vault = new[]
            {
                new VaultItem { Sha1Hash = null },
                new VaultItem { Sha1Hash = null }
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, VaultKey);
            Assert.Equal(0, result.ReusedCount);
        }

        [Fact]
        public void ComputeWarningsSync_HashCaseInsensitive_CountsAsReused()
        {
            var vault = new[]
            {
                new VaultItem { Sha1Hash = "5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8" },
                new VaultItem { Sha1Hash = "5baa61e4c9b93f3f0682250b6cf8331b7ee68fd8" }
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, VaultKey);
            Assert.Equal(2, result.ReusedCount);
        }

        // ── ComputeWarningsSync: old counter ──────────────────────────────────────

        [Fact]
        public void ComputeWarningsSync_PasswordOlderThan90Days_OldCountIsOne()
        {
            var item = MakeEncryptedItem("Str0ng!P@ssw0rd#", DateTime.UtcNow.AddDays(-100));
            var result = WarningsHelper.ComputeWarningsSync(new[] { item }, VaultKey);
            Assert.Equal(1, result.OldCount);
        }

        [Fact]
        public void ComputeWarningsSync_RecentPassword_OldCountIsZero()
        {
            var item = MakeEncryptedItem("Str0ng!P@ssw0rd#", DateTime.UtcNow.AddDays(-10));
            var result = WarningsHelper.ComputeWarningsSync(new[] { item }, VaultKey);
            Assert.Equal(0, result.OldCount);
        }

        [Fact]
        public void ComputeWarningsSync_DefaultLastUpdate_OldCountIsZero()
        {
            // Items with DateTime.MinValue LastUpdate are newly created and should be skipped.
            var item = new VaultItem { Sha1Hash = "abc", LastUpdate = default };
            var result = WarningsHelper.ComputeWarningsSync(new[] { item }, VaultKey);
            Assert.Equal(0, result.OldCount);
        }

        [Fact]
        public void ComputeWarningsSync_TwoOldTwoRecent_OldCountIsTwo()
        {
            var vault = new[]
            {
                MakeEncryptedItem("Str0ng!P@ss#1", DateTime.UtcNow.AddDays(-200)), // old
                MakeEncryptedItem("Str0ng!P@ss#2", DateTime.UtcNow.AddDays(-91)),  // old
                MakeEncryptedItem("Str0ng!P@ss#3", DateTime.UtcNow.AddDays(-30)),  // recent
                MakeEncryptedItem("Str0ng!P@ss#4", DateTime.UtcNow)               // just updated
            };
            var result = WarningsHelper.ComputeWarningsSync(vault, VaultKey);
            Assert.Equal(2, result.OldCount);
        }

        // ── GetItemsAtRiskAsync: weak ─────────────────────────────────────────────

        [Fact]
        public async System.Threading.Tasks.Task GetItemsAtRisk_Weak_ReturnsWeakItems()
        {
            var weak = MakeEncryptedItem("weakpassword"); // no upper/digit/special
            var strong = MakeEncryptedItem("Str0ng!P@ssw0rd#");

            var items = await WarningsHelper.GetItemsAtRiskAsync(
                new[] { weak, strong }, VaultKey, "weak");

            Assert.Single(items);
            Assert.Equal(weak.Id, items[0].Id);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetItemsAtRisk_Weak_NullVault_ReturnsEmpty()
        {
            var items = await WarningsHelper.GetItemsAtRiskAsync(null, VaultKey, "weak");
            Assert.Empty(items);
        }

        // ── GetItemsAtRiskAsync: reused ──────────────────────────────────────────

        [Fact]
        public async System.Threading.Tasks.Task GetItemsAtRisk_Reused_ReturnsBothReusedItems()
        {
            const string sha1 = "AABBCCDDEEFF00112233445566778899AABBCCDD";
            var item1 = new VaultItem { Id = 1, Sha1Hash = sha1 };
            var item2 = new VaultItem { Id = 2, Sha1Hash = sha1 };
            var unique = new VaultItem { Id = 3, Sha1Hash = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" };

            var items = await WarningsHelper.GetItemsAtRiskAsync(
                new[] { item1, item2, unique }, VaultKey, "reused");

            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetItemsAtRisk_Reused_EmptyVault_ReturnsEmpty()
        {
            var items = await WarningsHelper.GetItemsAtRiskAsync(
                new List<VaultItem>(), VaultKey, "reused");
            Assert.Empty(items);
        }

        // ── GetItemsAtRiskAsync: old ─────────────────────────────────────────────

        [Fact]
        public async System.Threading.Tasks.Task GetItemsAtRisk_Old_ReturnsOldItems()
        {
            var old = new VaultItem { Id = 1, LastUpdate = DateTime.UtcNow.AddDays(-200) };
            var recent = new VaultItem { Id = 2, LastUpdate = DateTime.UtcNow };

            var items = await WarningsHelper.GetItemsAtRiskAsync(
                new[] { old, recent }, VaultKey, "old");

            Assert.Single(items);
            Assert.Equal(old.Id, items[0].Id);
        }

        // ── GetItemsAtRiskAsync: unknown category ────────────────────────────────

        [Fact]
        public async System.Threading.Tasks.Task GetItemsAtRisk_UnknownCategory_ReturnsEmpty()
        {
            var vault = new[] { MakeEncryptedItem("Str0ng!P@ssw0rd#") };
            var items = await WarningsHelper.GetItemsAtRiskAsync(vault, VaultKey, "unknown");
            Assert.Empty(items);
        }
    }
}
