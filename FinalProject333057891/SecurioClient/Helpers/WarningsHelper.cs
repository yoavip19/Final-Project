using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SecurioClient.Helpers
{
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
        /// Analyses every item in <paramref name="vault"/> and returns aggregated
        /// warning counters. Password decryption (needed for the "weak" check)
        /// uses the provided <paramref name="vaultKey"/>.
        /// </summary>
        public static WarningsData ComputeWarnings(IList<VaultItem> vault, string vaultKey)
        {
            if (vault == null || vault.Count == 0)
                return new WarningsData();

            int leaked = 0;
            int weak = 0;
            int reused = 0;
            int old = 0;

            DateTime oldThreshold = DateTime.UtcNow.AddDays(-OldPasswordDays);

            // ── Leaked ────────────────────────────────────────
            // Uses the IsLeaked flag that the server sets via the HIBP
            // k-anonymity check when a password is added or updated.
            leaked = vault.Count(v => v.IsLeaked);

            // ── Weak ──────────────────────────────────────────
            // Decrypt each password and run it through the same
            // validation rules enforced on the signup page.
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

                    var result = ValidationHelper.ValidatePassword(plaintext);
                    if (!result.IsValid)
                        weak++;
                }
                catch
                {
                    // If decryption fails for any reason, skip the item.
                }
            }

            // ── Reused ────────────────────────────────────────
            // Two or more items that share the same SHA-1 hash are
            // reusing the same password.  Every member of such a
            // group is counted (not just the duplicates).
            var hashGroups = vault
                .Where(v => !string.IsNullOrEmpty(v.Sha1Hash))
                .GroupBy(v => v.Sha1Hash, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

            foreach (var group in hashGroups)
                reused += group.Count();

            // ── Old ───────────────────────────────────────────
            // Passwords whose LastUpdate is older than the threshold.
            // Items with a default (unset) timestamp are also counted
            // because the absence of a known update date means the
            // password has never been confirmed as recently changed.
            foreach (var item in vault)
            {
                if (item.LastUpdate == default || item.LastUpdate < oldThreshold)
                    old++;
            }

            return new WarningsData
            {
                LeakedCount = leaked,
                WeakCount = weak,
                ReusedCount = reused,
                OldCount = old
            };
        }
    }
}
