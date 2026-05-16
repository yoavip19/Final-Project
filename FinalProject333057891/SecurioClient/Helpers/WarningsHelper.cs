using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurioClient.Helpers
{
    /// <summary>Risk categories used to classify password-health warnings.</summary>
    public enum RiskCategory
    {
        Leaked,
        Weak,
        Reused,
        Old
    }

    /// <summary>
    /// Holds the four password-risk counters computed from the vault.
    /// </summary>
    public sealed class WarningsData
    {
        public int LeakedCount { get; set; }
        public int WeakCount { get; set; }
        public int ReusedCount { get; set; }
        public int OldCount { get; set; }
    }

    /// <summary>
    /// Computes password-health warning counters from the in-memory vault.
    /// Counters are designed to be calculated once at login and cached in
    /// <see cref="SessionHelper.CachedWarnings"/>; the cache is flushed
    /// whenever the vault contents change.
    /// </summary>
    public static class WarningsHelper
    {
        /// <summary>
        /// Best-practice password age threshold. NIST SP 800-63B does not mandate
        /// forced rotation, but 90 days is the widely adopted industry standard
        /// for flagging stale credentials in consumer password managers.
        /// </summary>
        private const int OldPasswordDays = 90;

        /// <summary>
        /// Synchronously computes warning counters from the vault using stored flags
        /// (no live HIBP network calls). The leaked count uses each item's stored
        /// <see cref="VaultItem.IsLeaked"/> flag, which is set by the server at
        /// add/edit time. All other checks are computed locally.
        /// </summary>
        public static WarningsData ComputeWarningsSync(IList<VaultItem> vault, string vaultKey)
        {
            if (vault == null || vault.Count == 0)
                return new WarningsData();

            int leaked = GetLeakedItems(vault).Count;
            int weak = GetWeakItems(vault, vaultKey).Count;
            int reused = GetReusedItems(vault).Count;
            int old = GetOldItems(vault).Count;

            return new WarningsData
            {
                LeakedCount = leaked,
                WeakCount   = weak,
                ReusedCount = reused,
                OldCount    = old
            };
        }

        /// <summary>
        /// Analyses every item in <paramref name="vault"/> and returns aggregated
        /// warning counters. Password decryption (needed for the "weak" check)
        /// uses the provided <paramref name="vaultKey"/>.
        /// The "leaked" check performs a live HIBP k-anonymity query for each
        /// password's SHA-1 hash.
        /// </summary>
        public static async Task<WarningsData> ComputeWarningsAsync(IList<VaultItem> vault, string vaultKey)
        {
            if (vault == null || vault.Count == 0)
                return new WarningsData();

            int leaked = 0;

            // -- Leaked ----------------------------------------
            // Query HIBP for each password's SHA-1 hash using the k-anonymity
            // model so only the first 5 characters are ever transmitted.
            // The IsLeaked flag on each item is updated so that subsequent
            // synchronous recomputations (ComputeWarningsSync) stay accurate.
            foreach (var item in vault)
            {
                if (string.IsNullOrEmpty(item.Sha1Hash))
                    continue;

                bool pwned = await HibpClientService.IsPasswordPwnedAsync(item.Sha1Hash);
                item.IsLeaked = pwned;
                if (pwned) leaked++;
            }

            return new WarningsData
            {
                LeakedCount = leaked,
                WeakCount   = GetWeakItems(vault, vaultKey).Count,
                ReusedCount = GetReusedItems(vault).Count,
                OldCount    = GetOldItems(vault).Count
            };
        }

        /// <summary>
        /// Returns the subset of <paramref name="vault"/> items that fall under
        /// the specified <paramref name="category"/> risk.
        /// </summary>
        public static Task<List<VaultItem>> GetItemsAtRisk(
            IList<VaultItem> vault, string vaultKey, RiskCategory category)
        {
            if (vault == null || vault.Count == 0)
                return System.Threading.Tasks.Task.FromResult(new List<VaultItem>());

            List<VaultItem> result;
            switch (category)
            {
                case RiskCategory.Leaked:
                    result = GetLeakedItems(vault);
                    break;
                case RiskCategory.Weak:
                    result = GetWeakItems(vault, vaultKey);
                    break;
                case RiskCategory.Reused:
                    result = GetReusedItems(vault);
                    break;
                case RiskCategory.Old:
                    result = GetOldItems(vault);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, $"Unknown risk category: {category}.");
            }

            return System.Threading.Tasks.Task.FromResult(result);
        }

        /// <summary>
        /// Returns the subset of vault items whose password appears in a known data breach,
        /// using the server-provided <see cref="VaultItem.IsLeaked"/> flag set at add/edit time.
        /// </summary>
        private static List<VaultItem> GetLeakedItems(IList<VaultItem> vault)
        {
            return vault.Where(item => item.IsLeaked).ToList();
        }

        /// <summary>Returns the subset of vault items that fail the password strength validation.</summary>
        private static List<VaultItem> GetWeakItems(IList<VaultItem> vault, string vaultKey)
        {
            var result = new List<VaultItem>();
            foreach (var item in vault)
            {
                try
                {
                    if (string.IsNullOrEmpty(item.IV) ||
                        string.IsNullOrEmpty(item.Tag) ||
                        string.IsNullOrEmpty(item.CipherText))
                        continue;

                    string plaintext = EncryptionHelper.DecryptAesGcm(
                        item.IV, item.Tag, item.CipherText, vaultKey);

                    var validationResult = ValidationHelper.ValidatePassword(plaintext);
                    if (!validationResult.IsValid)
                        result.Add(item);
                }
                catch
                {
                    // Skip items that cannot be decrypted.
                }
            }
            return result;
        }

        /// <summary>Returns the subset of vault items that share a SHA-1 hash with at least one other item.</summary>
        private static List<VaultItem> GetReusedItems(IList<VaultItem> vault)
        {
            var hashGroups = vault
                .Where(v => !string.IsNullOrEmpty(v.Sha1Hash))
                .GroupBy(v => v.Sha1Hash, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

            var result = new List<VaultItem>();
            foreach (var group in hashGroups)
                result.AddRange(group);

            return result;
        }

        /// <summary>Returns the subset of vault items whose password has not been changed in over 90 days.</summary>
        private static List<VaultItem> GetOldItems(IList<VaultItem> vault)
        {
            DateTime oldThreshold = DateTime.UtcNow.AddDays(-OldPasswordDays);
            return vault
                .Where(item => item.LastUpdate != default && item.LastUpdate < oldThreshold)
                .ToList();
        }
    }
}
