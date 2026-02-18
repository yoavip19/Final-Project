using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using SQLite;
using Android.Content;

namespace FinalProject333057891
{
    public static class GooglePlayAPI
    {
        private static readonly HttpClient client = new HttpClient();
        private const string SerpApiKey = MainActivity.Key;

        /// <summary>
        /// Searches Google Play Store and returns Application objects.
        /// Automatically saves results to database to reduce future API calls.
        /// </summary>
        /// <param name="query">Search query (app name)</param>
        /// <param name="context">Android context for database access</param>
        /// <returns>List of Application objects with complete data</returns>
        public static async Task<List<Application>> SearchAppsAsync(string query, Context context)
        {
            var results = new List<Application>();
            SQLiteConnection dbCommand = Helper.GetDBCommand(context);

            try
            {
                string url = $"https://serpapi.com/search?engine=google_play&q={Uri.EscapeDataString(query)}&store=apps&hl=en&gl=us&api_key={SerpApiKey}";

                var response = await client.GetStringAsync(url);
                var json = JObject.Parse(response);

                // Process highlight (exact match)
                var highlight = json["app_highlight"];
                if (highlight != null)
                {
                    var app = await ProcessSearchResult(
                        highlight["title"]?.ToString(),
                        highlight["product_id"]?.ToString(),
                        highlight["thumbnail"]?.ToString(),
                        highlight["genre"]?.ToString(),
                        dbCommand
                    );

                    if (app != null)
                        results.Add(app);
                }

                // Process organic results
                var organic = json["organic_results"] as JArray;
                if (organic != null)
                {
                    foreach (var section in organic)
                    {
                        var items = section["items"] as JArray;
                        if (items == null) continue;

                        foreach (var item in items)
                        {
                            var app = await ProcessSearchResult(
                                item["title"]?.ToString(),
                                item["product_id"]?.ToString(),
                                item["thumbnail"]?.ToString(),
                                item["genre"]?.ToString(),
                                dbCommand
                            );

                            if (app != null)
                                results.Add(app);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash - return whatever results we got
                System.Diagnostics.Debug.WriteLine($"SERP API Error: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Processes a single search result from SERP API.
        /// Validates required fields, downloads icon, creates Application object,
        /// and saves to database immediately.
        /// </summary>
        private static async Task<Application> ProcessSearchResult(
            string appName,
            string packageId,
            string iconUrl,
            string category,
            SQLiteConnection dbCommand)
        {
            // CRITICAL VALIDATION: Check all required fields
            // If any required field is missing, skip this result entirely
            if (string.IsNullOrEmpty(packageId))
            {
                System.Diagnostics.Debug.WriteLine("Skipping result: Missing PackageID");
                return null;
            }

            if (string.IsNullOrEmpty(appName))
            {
                System.Diagnostics.Debug.WriteLine($"Skipping result: Missing AppName for {packageId}");
                return null;
            }

            if (string.IsNullOrEmpty(iconUrl))
            {
                System.Diagnostics.Debug.WriteLine($"Skipping result: Missing Icon URL for {packageId}");
                return null;
            }

            // Download and convert icon to Base64
            string iconBase64;
            try
            {
                iconBase64 = await Application.DownloadImageAsBase64Async(iconUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to download icon for {packageId}: {ex.Message}");
                return null; // Can't proceed without icon
            }

            // Category is OPTIONAL - use default if missing
            if (string.IsNullOrEmpty(category))
            {
                category = "Uncategorized";
            }

            // Create Application object
            var application = new Application(packageId, appName, iconBase64, category);

            // IMMEDIATELY save to database to build up cache
            try
            {
                // Check if already exists
                var existing = dbCommand.Find<Application>(packageId);
                if (existing == null)
                {
                    dbCommand.Insert(application);
                    System.Diagnostics.Debug.WriteLine($"Saved new app to DB: {appName}");
                }
                else
                {
                    // Optionally update existing record with latest data
                    existing.AppName = appName;
                    existing.IconBase64 = iconBase64;
                    existing.Category = category;
                    dbCommand.Update(existing);
                    System.Diagnostics.Debug.WriteLine($"Updated existing app in DB: {appName}");
                }
            }
            catch (Exception ex)
            {
                // Don't fail if database save fails - still return the object
                System.Diagnostics.Debug.WriteLine($"DB save failed for {packageId}: {ex.Message}");
            }

            return application;
        }

        /// <summary>
        /// Gets detailed app metadata using the FREE Rajkumaar API.
        /// Use this for:
        /// - Manual package ID lookups
        /// - Validating/refreshing app data
        /// - When you have package ID but no other data
        /// </summary>
        public static async Task<Application> GetAppMetadataAsync(string packageId)
        {
            try
            {
                string url = $"https://play.rajkumaar.co.in/json?id={packageId}";
                var response = await client.GetStringAsync(url);
                var json = JObject.Parse(response);

                if (json["error"] != null) return null;

                string appName = json["name"]?.ToString();
                string iconUrl = json["logo"]?.ToString();
                string category = json["category"]?.ToString();

                // Validate required fields
                if (string.IsNullOrEmpty(packageId) ||
                    string.IsNullOrEmpty(appName) ||
                    string.IsNullOrEmpty(iconUrl))
                {
                    return null;
                }

                string iconBase64 = await Application.DownloadImageAsBase64Async(iconUrl);

                return new Application(
                    packageId,
                    appName,
                    iconBase64,
                    category ?? "Uncategorized"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Rajkumaar API Error: {ex.Message}");
                return null;
            }
        }
    }
}