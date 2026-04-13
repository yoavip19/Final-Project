using System;

namespace SecurioModels
{
    public class VaultItem
    {
        public int Id { get; set; }
        public string AppName { get; set; }
        public string AppUsername { get; set; }

        // Encrypted data stored as Base64 strings
        public string Ciphertext { get; set; }
        public string Iv { get; set; }
        public string Tag { get; set; }

        // Security metadata
        public string Sha1Hash { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
