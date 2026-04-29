using System;
using System.Collections.Generic;
using System.Text;

namespace SecurioModels.DataTransferObjects
{
    /// <summary>Represents a user account including security salts and hashed keys stored in the database.</summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string MasterPasswordKey { get; set; } // The key derivation result
        public string AuthSalt { get; set; }          // The salt for deriving the master password key
        public string EncryptionSalt { get; set; }    // The salt for the AES vault
        public DateTime LastLogin { get; set; }
        public DateTime LastPasswordUpdate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PasswordCount { get; set; }
    }
}