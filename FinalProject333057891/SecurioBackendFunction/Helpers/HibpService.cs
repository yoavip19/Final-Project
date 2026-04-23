namespace SecurioBackendFunction.Helpers
{
    /// <summary>Checks passwords against the Have I Been Pwned Pwned Passwords API using the k-anonymity model.</summary>
    public class HibpService : IHibpService
    {
        private readonly HttpClient _http;
        private const string ApiBase = "https://api.pwnedpasswords.com/range/";

        /// <summary>Initializes a new instance of HibpService.</summary>
        public HibpService(HttpClient http) => _http = http;

        /// <summary>Returns true if the given SHA-1 hex hash appears in the HIBP dataset.</summary>
        public async Task<bool> IsPasswordPwnedAsync(string sha1Hash)
        {
            if (string.IsNullOrWhiteSpace(sha1Hash) || sha1Hash.Length != 40)
                return false;

            string prefix = sha1Hash[..5].ToUpperInvariant();
            string suffix = sha1Hash[5..].ToUpperInvariant();

            try
            {
                string body = await _http.GetStringAsync(ApiBase + prefix);

                // Each line in the response is "SUFFIX:COUNT" (CRLF-terminated).
                foreach (string line in body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
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
