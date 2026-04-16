namespace SecurioClient
{
    /// <summary>
    /// Represents a single stored credential displayed in the vault RecyclerView.
    /// </summary>
    public class PasswordEntry
    {
        public int Id { get; set; }
        public string SiteName { get; set; }
        public string Username { get; set; }
        public string EncryptedPassword { get; set; }
    }
}
