using System;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using Xunit;
using ConsoleFunctionsCheck;

namespace TestProject
{
    public class DatabaseIntegrationTests : IDisposable
    {
        private readonly SQLiteConnection _db;

        public DatabaseIntegrationTests()
        {
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

            var pass = new Password("TestUser", "com.whatsapp", "WhatsUser", "Test123");
            _db.Insert(pass);

            var foundPass = _db.Table<Password>().FirstOrDefault(p => p.Username == "TestUser" && p.AppPackageID == "com.whatsapp");
            Assert.NotNull(foundPass);
            Assert.Equal("com.whatsapp", foundPass.AppPackageID);
        }

        public void Dispose()
        {
            _db.Close();
        }
    }

}