using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using SQLite;
using Xunit;
using Xunit.Abstractions;
using ConsoleFunctionsCheck;

namespace TestProject
{
    public class DatabaseIntegrationTests : IDisposable
    {
        private readonly SQLiteConnection _db;
        private readonly ITestOutputHelper _output;

        public DatabaseIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            // Use in-memory database for isolation
            _db = new SQLiteConnection(":memory:");
            _db.Execute("PRAGMA foreign_keys = ON;");
            _db.CreateTable<Application>();
            _db.Execute(@"
            CREATE TABLE IF NOT EXISTS Passwords (
                Username TEXT NOT NULL,
                AppPackageID TEXT NOT NULL,
                AppUsername TEXT,
                Salt TEXT,
                InitVector TEXT,
                PasswordEncrypted TEXT,
                IsFavorite INTEGER DEFAULT 0,
                PRIMARY KEY (Username, AppPackageID),
                FOREIGN KEY (AppPackageID) REFERENCES Applications(PackageID) ON DELETE CASCADE ON UPDATE CASCADE
            );
        ");
        }

        [Fact]
        public async Task Insert_Application_From_GooglePlayAPI_And_Password_Succeeds()
        {
            // Arrange
            var app = await GooglePlayAPI.GetAppMetadataAsync("com.whatsapp");
            Assert.NotNull(app);

            // Act
            _db.Insert(app);

            var foundApp = _db.Find<Application>("com.whatsapp");
            Assert.NotNull(foundApp);
            Assert.Equal("com.whatsapp", foundApp.PackageID);

            // Use the constructor that includes the IsFavorite flag
            var pass = new Password("TestUser", "com.whatsapp", "WhatsUser", "Test123", isFavorite: false);
            _db.Insert(pass);

            var foundPass = _db.Table<Password>().FirstOrDefault(p => p.Username == "TestUser" && p.AppPackageID == "com.whatsapp");
            Assert.NotNull(foundPass);
            Assert.Equal("com.whatsapp", foundPass.AppPackageID);
            Assert.False(foundPass.IsFavorite);
        }

        [Fact]
        public async Task Insert_Multiple_Passwords_With_Favorites_Succeeds()
        {
            // Arrange - fetch app metadata for com.whatsapp
            var app = await GooglePlayAPI.GetAppMetadataAsync("com.whatsapp");
            Assert.NotNull(app);

            // Act - insert application
            _db.Insert(app);

            // Create multiple password records for the same app (use different Username values because PK is (Username, AppPackageID))
            var passwords = new[]
            {
                new Password("UserA", "com.whatsapp", "whatsA", "PwdA", isFavorite: true),
                new Password("UserB", "com.whatsapp", "whatsB", "PwdB", isFavorite: false),
                new Password("UserC", "com.whatsapp", "whatsC", "PwdC", isFavorite: true),
                new Password("UserD", "com.whatsapp", "whatsD", "PwdD", isFavorite: false),
            };

            foreach (var p in passwords)
                _db.Insert(p);

            // Assert - total count for the app
            var allForApp = _db.Table<Password>().Where(p => p.AppPackageID == "com.whatsapp").ToList();
            Assert.Equal(passwords.Length, allForApp.Count);

            // Print password data to test output AND to console/trace so it's visible in different runners
            void WriteLine(string line)
            {
                try { _output?.WriteLine(line); } catch { /* ignore */ }
                try { Console.WriteLine(line); } catch { /* ignore */ }
                try { Trace.WriteLine(line); } catch { /* ignore */ }
            }

            WriteLine("Passwords for com.whatsapp:");
            foreach (var p in allForApp.OrderBy(p => p.Username))
            {
                var line =
                    $"Username: {p.Username}, AppUsername: {p.AppUsername}, IsFavorite: {p.IsFavorite}, " +
                    $"Salt: {(p.Salt ?? "<null>")}, InitVector: {(p.InitVector ?? "<null>")}, PasswordEncrypted: {(p.PasswordEncrypted ?? "<null>")}";

            // Assert - favorites vs non-favorites
            var favorites = allForApp.Where(p => p.IsFavorite).ToList();
                var nonFavorites = allForApp.Where(p => !p.IsFavorite).ToList();

                Assert.Equal(2, favorites.Count);
                Assert.Equal(2, nonFavorites.Count);

                // Verify favorites are marked correctly
                Assert.Contains(favorites, f => f.Username == "UserA");
                Assert.Contains(favorites, f => f.Username == "UserC");
                Assert.DoesNotContain(favorites, f => f.Username == "UserB");
                Assert.DoesNotContain(favorites, f => f.Username == "UserD");
            }

        public void Dispose()
        {
            _db.Close();
        }
    }

}