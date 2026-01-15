using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleFunctionsCheck
{
    public class Helper
    {
        private const string dbName = "dbTestConsole";
        public Helper()
        {

        }
        public static string Path()
        {
            //returns the path of the database
            try
            {
                string path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), Helper.dbName);
                return path;
            }
            catch
            {
                Console.WriteLine("Error retrieving path");
                return "Error";
            }
        }
        public static SQLiteConnection GetDBCommand()
        {
            //returns the dbCommand
            return new SQLiteConnection(Path());
        }
        public static void Initialize()
        {
            //initializes the database
            try
            {
                string path = Path();
                if (path == "Error") return;
                var dbCommand = new SQLiteConnection(path);
                // Enable foreign key constraints
                dbCommand.Execute("PRAGMA foreign_keys = ON;");
                // Create Application table first (parent)
                dbCommand.CreateTable<Application>();
                // Then create Password table (child)
                dbCommand.Execute(@"
                    CREATE TABLE IF NOT EXISTS Passwords (
                        Username TEXT NOT NULL,
                        AppPackageID TEXT NOT NULL,
                        AppUsername TEXT,
                        Salt TEXT,
                        InitVector TEXT,
                        PasswordEncrypted TEXT,
                        PRIMARY KEY (Username, AppPackageID),
                        FOREIGN KEY (AppPackageID) REFERENCES Applications(PackageID) ON DELETE CASCADE ON UPDATE CASCADE
                    );
                ");
            }
            catch
            {
                Console.WriteLine("Error initializing database");
            }
        }
    }
}