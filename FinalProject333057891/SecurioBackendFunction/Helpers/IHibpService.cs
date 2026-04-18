namespace SecurioBackendFunction.Helpers
{
    // Defines the contract for checking passwords against the Have I Been Pwned dataset.
    // Uses the k-anonymity model: only the first 5 characters of the SHA-1 hash are transmitted.
    public interface IHibpService
    {
        // Returns true if the provided SHA-1 hash appears in the HIBP Pwned Passwords dataset,
        // indicating the password has been leaked in a known data breach.
        // sha1Hash must be a 40-character hexadecimal string (case-insensitive), e.g. the output
        // of SHA-1 applied to the plaintext password before any key-derivation step.
        Task<bool> IsPasswordPwnedAsync(string sha1Hash);
    }
}
