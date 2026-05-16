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
        /// <summary>The PBKDF2-derived master password key.</summary>
        public string MasterPasswordKey { get; set; }
        /// <summary>The salt used to derive the master password key.</summary>
        public string AuthSalt { get; set; }
        /// <summary>The salt used for AES vault key derivation.</summary>
        public string EncryptionSalt { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime LastPasswordUpdate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PasswordCount { get; set; }
    }
}