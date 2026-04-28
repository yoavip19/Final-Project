using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SecurioClient.Helpers
{
    /// <summary>
    /// Client-side HIBP Pwned Passwords check using the k-anonymity model.
    /// Only the first 5 characters of the SHA-1 hash are ever transmitted
    /// to the remote API; the full hash never leaves the device.
    /// </summary>
    public static class HibpClientService
    {
        private const string ApiBase = "https://api.pwnedpasswords.com/range/";

        // HIBP requires a descriptive User-Agent; requests without one receive HTTP 403.
        // Timeout is capped at 10 seconds so a slow or unresponsive API never stalls the UI.
        private static readonly HttpClient _http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Securio/1.0");
            return client;
        }

        /// <summary>
        /// Returns <c>true</c> if the given 40-character uppercase SHA-1 hex hash
        /// appears in the HIBP Pwned Passwords dataset.
        /// Returns <c>false</c> on any error or timeout (fail-open), so a
        /// temporary outage never produces false positives.
        /// </summary>
        public static async Task<bool> IsPasswordPwnedAsync(string sha1Hash)
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
            catch (OperationCanceledException)
            {
                // Timeout expired — fail open so a slow API never stalls the UI.
                return false;
            }
            catch
            {
                // Network or API error — fail open.
                return false;
            }
        }
    }
}
