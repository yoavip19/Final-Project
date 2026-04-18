using System;

namespace SecurioModels.DataTransferObjects
{
    // VaultItem - The secure data carrier for a stored credential.
    // IV, Tag, and CipherText are the AES-GCM components of the encrypted password.
    // Sha1Hash is an unsalted SHA-1 hash of the plaintext password used for HIBP breach checking.
    public class VaultItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string AccountName { get; set; }
        public string AccountUsername { get; set; }
        public string IV { get; set; }           // AES-GCM initialisation vector
        public string Tag { get; set; }          // AES-GCM authentication tag
        public string CipherText { get; set; }   // AES-GCM encrypted password
        public string Notes { get; set; }
        public string Sha1Hash { get; set; }     // Unsalted SHA-1 hash for HIBP breach lookup
        public bool IsLeaked { get; set; }
        public DateTime LastUpdate { get; set; }
        // Transient flag set by the client to indicate whether the password ciphertext was changed.
        // Never persisted to the database; used only during the UpdateVaultItem request.
        public bool PasswordChanged { get; set; }
    }
}
