using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SecurioClient.Helpers
{
    /// <summary>Client-side HIBP Pwned Passwords check using the k-anonymity model where only the first 5 characters of the SHA-1 hash are transmitted.</summary>
    public static class HibpClientService
    {
        private const string ApiBase = "https://api.pwnedpasswords.com/range/";

        // Single shared HttpClient — Xamarin.Android.Net.AndroidClientHandler (set via
        // AndroidHttpClientHandlerType in the .csproj) is used automatically on Android.
        /// <summary>Shared HTTP client for communicating with the HIBP Pwned Passwords API.</summary>
        private static readonly HttpClient _http = new HttpClient();

        /// <summary>Returns true if the given 40-character uppercase SHA-1 hex hash appears in the HIBP Pwned Passwords dataset; returns false on any error (fail-open).</summary>
        public static async Task<bool> IsPasswordPwnedAsync(string sha1Hash)
        {
            if (string.IsNullOrWhiteSpace(sha1Hash) || sha1Hash.Length != 40)
                return false;

            string upper  = sha1Hash.ToUpperInvariant();
            string prefix = upper[..5];
            string suffix = upper[5..];

            try
            {
                // Build the request with the User-Agent set on the message itself.
                // Per-request headers on HttpRequestMessage are reliably forwarded by
                // AndroidClientHandler, matching the pattern used in BaseService.cs.
                var request = new HttpRequestMessage(HttpMethod.Get, ApiBase + prefix);
                request.Headers.TryAddWithoutValidation("User-Agent", "Securio/1.0");

                var response = await _http.SendAsync(request);

                // If the API is unavailable or returns a non-success status, fail open.
                if (!response.IsSuccessStatusCode)
                    return false;

                string body = await response.Content.ReadAsStringAsync();

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
                // Network or API error — fail open so no exception ever escapes to the caller.
                return false;
            }
        }
    }
}
