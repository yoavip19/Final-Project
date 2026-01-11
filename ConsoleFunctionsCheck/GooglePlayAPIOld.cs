using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleFunctionsCheck
{
    public class GooglePlayAPIOld
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<List<AppSearchResult>> SearchAppsAsync(string query)
        {
            var results = new List<AppSearchResult>();
            string apiKey = "7607fe5ec0ffb1d79744ef9d6e87b5b372360197030129292e46937a979f4aa2"; // replace with your SerpAPI key

            // Build SerpAPI query URL
            string url =
                $"https://serpapi.com/search?engine=google_play&q={Uri.EscapeDataString(query)}&store=apps&gl=us&hl=en&api_key={apiKey}";

            var response = await client.GetStringAsync(url);
            var json = JObject.Parse(response);

            // 1) Check for a single app highlight (when around specific app)
            var highlight = json["app_highlight"];
            if (highlight != null)
            {
                results.Add(new AppSearchResult
                {
                    AppName = highlight["title"]?.ToString() ?? "",
                    PackageId = highlight["product_id"]?.ToString() ?? "",
                    IconUrl = highlight["thumbnail"]?.ToString() ?? ""
                });
            }

            // 2) Loop through organic results
            var organic = json["organic_results"] as JArray;
            if (organic != null)
            {
                foreach (var section in organic)
                {
                    // Some sections have "items" with multiple apps
                    var items = section["items"] as JArray;
                    if (items != null)
                    {
                        foreach (var app in items)
                        {
                            results.Add(new AppSearchResult
                            {
                                AppName = app["title"]?.ToString() ?? "",
                                PackageId = app["product_id"]?.ToString() ?? "",
                                IconUrl = app["thumbnail"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return results;
        }
    }
}
public class AppSearchResult
{
    public string PackageId { get; set; } = "";
    public string AppName { get; set; } = "";
    public string IconUrl { get; set; } = "";
}
