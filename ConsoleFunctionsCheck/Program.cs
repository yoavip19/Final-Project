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
internal class Program
{
    public static async Task Main(string[] args)
    {
        //name - 
        //pack - 
        //URL - 
        //Application whatsapp = new Application("WhatsApp Messenger", "com.whatsapp", "https://play-lh.googleusercontent.com/bYtqbOcTYOlgc6gqZ2rwb8lptHuwlNE75zYJu6Bn076-hTmvd96HH-6v7S0YUAAJXoJN=s64", "Communication");


        Helper.Initialize(); //error - password has 2 PKs //patched
        Application? app = await GooglePlayAPI.GetAppMetadataAsync("com.whatsapp");
        if (app == null) { return; }

        Password pass = new Password("Loser1", app.PackageID, "WhatsLoser", "Loser123", true);
        SQLiteConnection dbCommand = Helper.GetDBCommand();
        if (dbCommand.Find<Application>(app.PackageID) == null)
        {
            dbCommand.Insert(app);
        }

        var existingPassword = dbCommand.Table<Password>().FirstOrDefault(p => p.Username == pass.Username && p.AppPackageID == pass.AppPackageID);
        if (existingPassword == null)
        {
            dbCommand.Insert(pass);
        }

        string q = "SELECT * FROM Applications";
        List<Application> apps = dbCommand.Query<Application>(q);
        //Console.WriteLine($"Name {apps[0].AppName} -> PackID {apps[0].PackageID} -> IconBase64 {apps[0].IconBase64} -> Cat {apps[0].Category}");
        Console.WriteLine();

        q = "SELECT * FROM Passwords WHERE Username = 'Loser1';";
        List<Password> passes = dbCommand.Query<Password>(q);
        Console.WriteLine(passes[0].PasswordEncrypted.Length);
        Console.WriteLine($"Uname {passes[0].Username} -> PackID {passes[0].AppPackageID} -> Appuname {passes[0].AppUsername} -> PassEnc {passes[0].PasswordEncrypted} -> Salt {passes[0].Salt} -> IV {passes[0].InitVector} -> Favorite {passes[0].IsFavorite}");
        Console.WriteLine(SecurityHelper.DecryptAES(passes[0].PasswordEncrypted, "Insert master", passes[0].Salt, passes[0].InitVector));
    }
    public static async Task Lbozo()
    {
        Console.WriteLine("Type app name fragment:");
        string input = Console.ReadLine() ?? "";

        var apps = await GooglePlayAPI.SearchAppsAsync(input);

        if (apps.Count == 0)
        {
            Console.WriteLine("No matches found.");
            return;
        }

        Console.WriteLine("Suggestions:");
        foreach (var app in apps)
        {
            //Console.WriteLine($"{app.AppName} -> {app.PackageId} -> {app.IconUrl}");
        }
    }
}