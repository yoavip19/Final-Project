using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Helpers
{
    /// <summary>
    /// Checks passwords against the Have I Been Pwned Pwned Passwords API using k-anonymity
    /// (only the first 5 hex characters of the SHA-1 hash are sent over the network).
    /// </summary>
    public class HibpService : IHibpService
    {
        private const string BaseUrl = "https://api.pwnedpasswords.com/range/";
        private readonly HttpClient _http;

        /// <summary>Initializes a new instance of HibpService with the provided HttpClient.</summary>
        public HibpService(HttpClient http) => _http = http;

        /// <summary>
        /// Returns true if the given SHA-1 hash appears in the HIBP breach database.
        /// Invalid or empty hashes always return false (fail open).
        /// </summary>
        public async Task<bool> IsPasswordPwnedAsync(string sha1Hash)
        {
            if (string.IsNullOrWhiteSpace(sha1Hash) || sha1Hash.Length != 40)
                return false;

            try
            {
                string upper  = sha1Hash.ToUpperInvariant();
                string prefix = upper[..5];
                string suffix = upper[5..];

                string body = await _http.GetStringAsync(BaseUrl + prefix);

                foreach (var line in body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int colon = line.IndexOf(':');
                    if (colon < 0) continue;
                    if (string.Equals(line.Substring(0, colon), suffix, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
            catch
            {
                // Fail open: a network error must never block a legitimate user.
                return false;
            }
        }
    }
}
