namespace SecurioBackendFunction.Helpers
{
    // Defines the contract for checking passwords against the Have I Been Pwned dataset.
    // Uses the k-anonymity model: only the first 5 characters of the SHA-1 hash are transmitted.
    public interface IHibpService
    {
        // Returns true if the provided SHA-1 hash (40-character hex string) appears
        // in the HIBP Pwned Passwords dataset, indicating the password has been leaked.
        Task<bool> IsPasswordPwnedAsync(string sha1Hash);
    }
}
