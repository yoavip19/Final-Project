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
                dbCommand.CreateTable<Application>();

                dbCommand.CreateTable<Password>();
            }
            catch
            {
                Console.WriteLine("Error initializing database");
            }
        }
    }
}