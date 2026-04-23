using System;

namespace SecurioModels.DataTransferObjects
{
    /// <summary>Represents a single entry in the master-password history log.</summary>
    public class MasterPasswordHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        // The PBKDF2-derived key that was stored in Users.MasterPasswordKey at the time.
        public string PasswordKey { get; set; }
        // The AuthSalt that was used to derive PasswordKey — needed by the client for comparison.
        public string AuthSalt { get; set; }
        // The date when this password was originally created (equals Users.LastPasswordUpdate at time of archival).
        public DateTime CreatedAt { get; set; }
    }
}
