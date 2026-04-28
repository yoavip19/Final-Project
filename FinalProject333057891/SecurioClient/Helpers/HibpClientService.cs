using System;
using System.Net.Http;
using System.Threading;
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
        private const int TimeoutSeconds = 10;

        // Single shared HttpClient — Xamarin.Android.Net.AndroidClientHandler (set in
        // AndroidHttpClientHandlerType) is used automatically for new HttpClient() on Android.
        private static readonly HttpClient _http = new HttpClient();

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

            string upper  = sha1Hash.ToUpperInvariant();
            string prefix = upper[..5];
            string suffix = upper[5..];

            try
            {
                // Use SendAsync + HttpRequestMessage so the User-Agent is set per-request.
                // AndroidClientHandler (the default handler on Android) does not reliably
                // forward HttpClient.DefaultRequestHeaders to the underlying Java HTTP stack,
                // and HttpClient.Timeout is not honoured by AndroidClientHandler either.
                // Setting the header on each HttpRequestMessage and using a CancellationToken
                // for the timeout both work correctly with AndroidClientHandler.
                using var cts     = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                using var request = new HttpRequestMessage(HttpMethod.Get, ApiBase + prefix);
                request.Headers.TryAddWithoutValidation("User-Agent", "Securio/1.0");

                using var response = await _http.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

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
