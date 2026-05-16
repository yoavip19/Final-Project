using System;

namespace SecurioModels.DataTransferObjects
{
    /// <summary>The secure data carrier for a stored credential with AES-GCM encryption components.</summary>
    public class VaultItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string AccountName { get; set; }
        public string AccountUsername { get; set; }
        /// <summary>AES-GCM initialization vector.</summary>
        public string IV { get; set; }
        /// <summary>AES-GCM authentication tag.</summary>
        public string Tag { get; set; }
        /// <summary>AES-GCM encrypted ciphertext.</summary>
        public string CipherText { get; set; }
        public string Notes { get; set; }
        /// <summary>Unsalted SHA-1 hash used for HIBP breach lookup.</summary>
        public string Sha1Hash { get; set; }
        public bool IsLeaked { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
