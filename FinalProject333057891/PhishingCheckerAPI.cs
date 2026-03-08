using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FinalProject333057891
{
    public static class PhishingCheckerAPI
    {
        private static readonly HttpClient client = new HttpClient();
        private const string ApiKey = MainActivity.SafeBrowsingKey;
        // v5 endpoint - broader and more up-to-date threat database than v4
        private const string Endpoint = "https://safebrowsing.googleapis.com/v5/uris:batchGet?key=";

        /// <summary>
        /// Checks a URL against Google Safe Browsing API v5.
        /// Returns (true, null) if safe, (false, threatType) if flagged.
        /// Throws an exception with the full error body if the request fails.
        /// </summary>
        public static async Task<(bool IsSafe, string ThreatType)> CheckUrlAsync(string url)
        {
            var requestBody = new JObject
            {
                ["uris"] = new JArray { url },
                ["threatTypes"] = new JArray
                {
                    "MALWARE",
                    "SOCIAL_ENGINEERING",
                    "UNWANTED_SOFTWARE",
                    "POTENTIALLY_HARMFUL_APPLICATION"
                }
            };

            var content = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Endpoint + ApiKey, content);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {(int)response.StatusCode}: {errorBody}");
            }

            string responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);

            // v5 returns a "threats" array; empty or missing means safe
            var threats = json["threats"];
            if (threats == null || !threats.HasValues)
                return (true, null);

            string threatType = threats[0]?["threatTypes"]?[0]?.ToString() ?? "UNKNOWN";
            return (false, threatType);
        }
    }
}