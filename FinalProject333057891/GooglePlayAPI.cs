using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleFunctionsCheck
{
    public static class GooglePlayAPI
    {
        private static readonly HttpClient client = new HttpClient();
        private const string SERP_API_KEY = "7607fe5ec0ffb1d79744ef9d6e87b5b372360197030129292e46937a979f4aa2";

        public static async Task<List<AppSuggestion>> SearchAppsAsync(string query)
        {
            var results = new List<AppSuggestion>();

            string url =
                $"https://serpapi.com/search?engine=google_play&q={Uri.EscapeDataString(query)}&store=apps&hl=en&gl=us&api_key={SERP_API_KEY}";

            var response = await client.GetStringAsync(url);
            var json = JObject.Parse(response);

            // Highlight (exact match)
            var highlight = json["app_highlight"];
            if (highlight != null)
            {
                results.Add(new AppSuggestion
                {
                    AppName = highlight["title"]?.ToString() ?? "",
                    PackageId = highlight["product_id"]?.ToString() ?? "",
                    IconUrl = highlight["thumbnail"]?.ToString() ?? ""
                });
            }

            // Organic results
            var organic = json["organic_results"] as JArray;
            if (organic != null)
            {
                foreach (var section in organic)
                {
                    var items = section["items"] as JArray;
                    if (items == null) continue;

                    foreach (var app in items)
                    {
                        string name = app["title"]?.ToString() ?? "";
                        string packageId = app["product_id"]?.ToString() ?? "";
                        string icon = app["thumbnail"]?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(packageId))
                            results.Add(new AppSuggestion
                            {
                                AppName = name,
                                PackageId = packageId,
                                IconUrl = icon
                            });
                    }
                }
            }

            return results;
        }

        public static async Task<Application?> GetAppMetadataAsync(string packageId)
        {
            try
            {
                string url = $"https://play.rajkumaar.co.in/json?id={packageId}";
                var response = await client.GetStringAsync(url);
                var json = JObject.Parse(response);

                if (json["error"] != null) return null;

                string appName = json["name"]?.ToString() ?? "";
                string iconUrl = json["logo"]?.ToString() ?? "";
                string iconBase64 = await Application.DownloadImageAsBase64Async(iconUrl);
                string category = json["category"]?.ToString() ?? "";
                return new Application(packageId, appName, iconBase64, category);
            }
            catch
            {
                return null;
            }
        }

    }
    // Autocomplete suggestion
    public class AppSuggestion
    {
        public string AppName { get; set; } = "";
        public string PackageId { get; set; } = "";
        public string IconUrl { get; set; } = "";
    }
}
