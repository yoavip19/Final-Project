using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject333057891
{
    public class Helper
    {
        private const string dbName = "dbTest0";
        public Helper()
        {

        }
        public static string Path(Context context)
        {
            //returns the path of the database
            try
            {
                string path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), Helper.dbName);
                return path;
            }
            catch
            {
                Toast.MakeText(context, "Error retrieving path", ToastLength.Short).Show();
                return "Error";
            }
        }
        public static SQLiteConnection GetDBCommand(Context context)
        {
            //returns the dbCommand
            return new SQLiteConnection(Path(context));
        }
        public static void Initialize(Context context)
        {
            //initializes the database
            try
            {
                string path = Path(context);
                if (path == "Error") return;
                var dbCommand = new SQLiteConnection(path);

                // Enable foreign key constraints
                dbCommand.Execute("PRAGMA foreign_keys = ON;");
                dbCommand.CreateTable<User>();
                dbCommand.CreateTable<Application>();
                dbCommand.Execute(@"
                    CREATE TABLE IF NOT EXISTS Passwords (
                        Username TEXT NOT NULL,
                        AppPackageID TEXT NOT NULL,
                        AppUsername TEXT,
                        Salt TEXT,
                        InitVector TEXT,
                        PasswordEncrypted TEXT,
                        IsFavorite BOOL,
                        PRIMARY KEY (Username, AppPackageID),
                        FOREIGN KEY (Username) REFERENCES Users(Username) ON DELETE CASCADE ON UPDATE CASCADE,
                        FOREIGN KEY (AppPackageID) REFERENCES Applications(PackageID) ON DELETE CASCADE ON UPDATE CASCADE
                    );
                ");
            }
            catch
            {
                Toast.MakeText(context, "Error initializing database", ToastLength.Short).Show();
            }
        }
    }
}