namespace SecurioModels.DataTransferObjects
{
    /// <summary>Request body for the UpdateVaultItem endpoint that carries the PasswordChanged flag separately from the vault item payload.</summary>
    public class UpdateVaultItemRequest
    {
        /// <summary>The vault item data to persist.</summary>
        public VaultItem Item { get; set; }

        /// <summary>True when the password ciphertext was changed by the user; when true the server updates the LastUpdate timestamp.</summary>
        public bool PasswordChanged { get; set; }
    }
}
