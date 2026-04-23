namespace SecurioBackendFunction.Helpers
{
    /// <summary>Defines the contract for checking passwords against the Have I Been Pwned dataset.</summary>
    public interface IHibpService
    {
        /// <summary>Returns true if the provided SHA-1 hash appears in the HIBP Pwned Passwords dataset.</summary>
        Task<bool> IsPasswordPwnedAsync(string sha1Hash);
    }
}
