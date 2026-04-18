namespace SecurioBackendFunction.Helpers
{
    // Checks passwords against the Have I Been Pwned Pwned Passwords API.
    // Uses the k-anonymity model so the full hash is never sent over the network:
    // only the first 5 hex characters are transmitted to the HIBP endpoint.
    public class HibpService : IHibpService
    {
        private readonly HttpClient _http;
        private const string ApiBase = "https://api.pwnedpasswords.com/range/";

        public HibpService(HttpClient http) => _http = http;

        // Returns true if the given SHA-1 hex hash appears in the HIBP dataset.
        // If the HIBP API is unreachable the method returns false (fail-open) so
        // a temporary outage never blocks legitimate user registrations.
        public async Task<bool> IsPasswordPwnedAsync(string sha1Hash)
        {
            if (string.IsNullOrWhiteSpace(sha1Hash) || sha1Hash.Length < 6)
                return false;

            string prefix = sha1Hash[..5].ToUpperInvariant();
            string suffix = sha1Hash[5..].ToUpperInvariant();

            try
            {
                string body = await _http.GetStringAsync(ApiBase + prefix);

                // Each line in the response is "SUFFIX:COUNT".
                foreach (string line in body.Split('\n'))
                {
                    int colon = line.IndexOf(':');
                    if (colon < 0) continue;
                    if (string.Equals(line[..colon].Trim(), suffix, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
            catch
            {
                // Fail open: if the HIBP service is unavailable, allow the operation to proceed.
                return false;
            }
        }
    }
}
