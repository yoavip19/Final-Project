//from: yudbet4ironia@gmail.com
//app pass: qhip imme dcek jgus

using ConsoleFunctionsCheck;
using FinalProject333057891;
using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Security;
using SQLite;
using System.Data.Common;
internal class Program
{
    public static async Task Main(string[] args)
    {
        //name - 
        //pack - 
        //URL - 
        //Application whatsapp = new Application("WhatsApp Messenger", "com.whatsapp", "https://play-lh.googleusercontent.com/bYtqbOcTYOlgc6gqZ2rwb8lptHuwlNE75zYJu6Bn076-hTmvd96HH-6v7S0YUAAJXoJN=s64", "Communication");


        Helper.Initialize(); //error - password has 2 PKs //patched

        var popularApps = new List<string>
        {
            "com.whatsapp",                // WhatsApp
            "com.instagram.android",       // Instagram
            "com.facebook.katana",         // Facebook
            "com.zhiliaoapp.musically",    // TikTok
            "com.snapchat.android",        // Snapchat
            "org.telegram.messenger",      // Telegram
            "com.spotify.music",           // Spotify
            "com.netflix.mediaclient",     // Netflix
            "com.google.android.youtube",  // YouTube
            "com.twitter.android",         // Twitter / X
            "com.waze",                    // Waze
            "com.google.android.gm",       // Gmail
            "com.amazon.mShop.android.shopping", // Amazon Shopping
            "com.discord",                 // Discord
            "com.pinterest",               // Pinterest
            "com.linkedin.android",        // LinkedIn
            "com.ubercab",                 // Uber
            "com.paypal.android.p2pmobile",// PayPal
            "com.microsoft.teams",         // Microsoft Teams
            "zoom.us.google"               // Zoom
        };

        // Loop through the list and add them one by one
        foreach (var packageId in popularApps)
        {
            await AddAppSafeAsync(packageId);
        }
        SQLiteConnection dbCommand = Helper.GetDBCommand();

        string q = "SELECT * FROM Applications";
        List<Application> apps = dbCommand.Query<Application>(q);
        Console.WriteLine($"Name {apps[2].AppName} -> PackID {apps[2].PackageID} -> IconBase64 {apps[2].IconBase64} -> Cat {apps[2].Category}");
        Console.WriteLine();
    }
    private static async Task AddAppSafeAsync(string packageId)
    {
        try
        {
            SQLiteConnection dbCommand = Helper.GetDBCommand();
            // 1. Fetch from API
            var app = await GooglePlayAPI.GetAppMetadataAsync(packageId);

            // 2. Validate
            if (app == null) return;

            // 3. Insert if not exists
            if (dbCommand.Find<Application>(app.PackageID) == null)
            {
                dbCommand.Insert(app);
                Console.WriteLine($"[Seed] Added: {app.AppName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seed] Failed to add {packageId}: {ex.Message}");
        }
    }
    //public static async Task Lbozo()
    //{
    //    Console.WriteLine("Type app name fragment:");
    //    string input = Console.ReadLine() ?? "";

    //    var apps = await GooglePlayAPI.SearchAppsAsync(input);

    //    if (apps.Count == 0)
    //    {
    //        Console.WriteLine("No matches found.");
    //        return;
    //    }

    //    Console.WriteLine("Suggestions:");
    //    foreach (var app in apps)
    //    {
    //        //Console.WriteLine($"{app.AppName} -> {app.PackageId} -> {app.IconUrl}");
    //    }
    //}
}