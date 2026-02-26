using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FinalProject333057891
{
    public static class PhishingCheckerAPI
    {
        private static readonly HttpClient client = new HttpClient();
        private const string ApiKey = MainActivity.SafeBrowsingKey;
        private const string Endpoint = "https://safebrowsing.googleapis.com/v4/threatMatches:find?key=";

        /// <summary>
        /// Checks a URL against Google Safe Browsing API.
        /// Returns true if the URL is safe, false if it is flagged as a threat.
        /// Throws an exception if the request fails.
        /// </summary>
        public static async Task<(bool IsSafe, string ThreatType)> CheckUrlAsync(string url)
        {
            string requestBody = BuildRequestBody(url);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(Endpoint + ApiKey, content);
            response.EnsureSuccessStatusCode();

            string responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);

            // If the response has no "matches" field, the URL is safe
            var matches = json["matches"];
            if (matches == null || !matches.HasValues)
                return (true, null);

            // Extract the first threat type found
            string threatType = matches[0]?["threatType"]?.ToString() ?? "UNKNOWN";
            return (false, threatType);
        }

        private static string BuildRequestBody(string url)
        {
            return $@"{{
            ""client"": {{
                ""clientId"": ""FinalProject333057891"",
                ""clientVersion"": ""1.0""
            }},
            ""threatInfo"": {{
                ""threatTypes"": [""MALWARE"", ""SOCIAL_ENGINEERING"", ""UNWANTED_SOFTWARE"", ""POTENTIALLY_HARMFUL_APPLICATION""],
                ""platformTypes"": [""ANDROID""],
                ""threatEntryTypes"": [""URL""],
                ""threatEntries"": [
                    {{ ""url"": ""{url}"" }}
                ]
            }}
        }}";
        }
    }
}