namespace SecurioModels.DataTransferObjects
{
    /// <summary>
    /// Request body for the UpdateVaultItem endpoint.
    /// Wraps the vault item payload and carries the <see cref="PasswordChanged"/> flag
    /// separately so that transient client-side state is never mixed into the persisted DTO.
    /// </summary>
    public class UpdateVaultItemRequest
    {
        /// <summary>The vault item data to persist.</summary>
        public VaultItem Item { get; set; }

        /// <summary>
        /// True when the password ciphertext was changed by the user.
        /// When true the server updates the <c>LastUpdate</c> timestamp on the item.
        /// </summary>
        public bool PasswordChanged { get; set; }
    }
}
