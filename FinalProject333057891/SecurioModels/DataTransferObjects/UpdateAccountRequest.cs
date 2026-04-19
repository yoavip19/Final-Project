using System.Collections.Generic;

namespace SecurioModels.DataTransferObjects
{
    // Carries the data needed for updating a user's account details.
    // When PasswordChanged is true, MasterPasswordKey, AuthSalt, EncryptionSalt,
    // and VaultItems (re-encrypted) must be provided.
    public class UpdateAccountRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public bool PasswordChanged { get; set; }

        // Only populated when PasswordChanged == true
        public string MasterPasswordKey { get; set; }
        public string AuthSalt { get; set; }
        public string EncryptionSalt { get; set; }

        // Re-encrypted vault items — sent when the master password changes
        // so the server can bulk-update them in one transaction.
        public List<VaultItem> VaultItems { get; set; }
    }
}
