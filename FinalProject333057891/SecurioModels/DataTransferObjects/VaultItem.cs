using System;

namespace SecurioModels.DataTransferObjects
{
    // VaultItem - The secure data carrier for a stored credential, containing the associated user ID, account metadata, and the already-encrypted password ciphertext.
    public class VaultItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string AccountName { get; set; }
        public string AccountUrl { get; set; }
        public string AccountUsername { get; set; }
        public string EncryptedPassword { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
