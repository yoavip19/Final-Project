using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace FinalProject333057891
{
    public static class GooglePlayAPI
    {
        private static readonly HttpClient client = new HttpClient();
        private const string SERP_API_KEY = MainActivity.Key;

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
                    PackageID = highlight["product_id"]?.ToString() ?? "",
                    IconBase64 = await Application.DownloadImageAsBase64Async(highlight["thumbnail"]?.ToString() ?? "")
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
                                PackageID = packageId,
                                IconBase64 = await Application.DownloadImageAsBase64Async(icon)
                            });
                    }
                }
            }

            return results;
        }

        public static async Task<Application> GetAppMetadataAsync(string packageId)
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
}
