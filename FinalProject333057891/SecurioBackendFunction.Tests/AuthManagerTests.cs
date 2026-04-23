using Moq;
using Xunit;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests
{
    // Comprehensive unit tests for AuthManager.
    // Covers RegisterAsync, VerifyLoginAsync, and GetUserSaltsAsync — every
    // success path, every validation rule, and every error branch.
    // All external dependencies (IUserRepository, IHibpService) are mocked.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class AuthManagerTests
    {
        static AuthManagerTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        // SHA-1 of the empty string — a real HIBP-leaked hash used as the "pwned" fixture.
        private const string PwnedHash = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";
        // A valid 40-char hex string that will NOT appear in any mocked HIBP response.
        private const string SafeHash  = "AABBCCDDEEFF00112233445566778899AABBCCDD";

        private static User ValidUser(string? sha1Hash = null) => new User
        {
            Username          = "alice",
            Email             = "alice@example.com",
            MasterPasswordKey = "derivedkey==",
            AuthSalt          = "authsalt==",
            EncryptionSalt    = "encsalt==",
            PasswordSha1Hash  = sha1Hash
        };

        private static User StoredUser() => new User
        {
            Id                = 7,
            Username          = "alice",
            Email             = "alice@example.com",
            MasterPasswordKey = "correct-hashed-key",
            AuthSalt          = "authsalt==",
            EncryptionSalt    = "encsalt=="
        };

        private static AuthManager Build(Mock<IUserRepository> repo, Mock<IHibpService>? hibp = null)
        {
            hibp ??= new Mock<IHibpService>();
            return new AuthManager(repo.Object, hibp.Object);
        }

        // ── RegisterAsync: happy path ────────────────────────────────────────────

        [Fact]
        public async Task Register_HappyPath_ReturnsSuccess()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(5);

            var result = await Build(repo, hibp).RegisterAsync(ValidUser(SafeHash));

            Assert.True(result.Success);
            Assert.Equal("User registered successfully", result.Message);
        }

        [Fact]
        public async Task Register_HappyPath_DataContainsToken()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(5);

            var result = await Build(repo, hibp).RegisterAsync(ValidUser(SafeHash));

            Assert.NotNull(result.Data);
            Assert.False(string.IsNullOrEmpty(result.Data!.Token));
        }

        [Fact]
        public async Task Register_HappyPath_DataContainsAssignedUserId()
        {
            const int assignedId = 99;
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(assignedId);

            var result = await Build(repo, hibp).RegisterAsync(ValidUser(SafeHash));

            Assert.Equal(assignedId, result.Data!.UserId);
        }

        [Fact]
        public async Task Register_HappyPath_DataContainsUsername()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            var user = ValidUser(SafeHash);
            var result = await Build(repo, hibp).RegisterAsync(user);

            Assert.Equal(user.Username, result.Data!.Username);
        }

        // ── RegisterAsync: HIBP breach check ────────────────────────────────────

        [Fact]
        public async Task Register_PwnedPassword_ReturnsFail()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var result = await Build(new Mock<IUserRepository>(), hibp).RegisterAsync(ValidUser(PwnedHash));

            Assert.False(result.Success);
            Assert.Contains("data breach", result.Message);
        }

        [Fact]
        public async Task Register_PwnedPassword_DatabaseNeverCalled()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);
            var repo = new Mock<IUserRepository>();

            await Build(repo, hibp).RegisterAsync(ValidUser(PwnedHash));

            repo.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Register_NullOrWhitespaceHash_SkipsHibpCheck(string? hash)
        {
            var hibp = new Mock<IHibpService>();
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            await Build(repo, hibp).RegisterAsync(ValidUser(hash));

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Register_SafeHash_HibpCalledExactlyOnce()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            await Build(repo, hibp).RegisterAsync(ValidUser(SafeHash));

            hibp.Verify(h => h.IsPasswordPwnedAsync(SafeHash), Times.Once);
        }

        // ── RegisterAsync: email uniqueness ─────────────────────────────────────

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsFail()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var result = await Build(repo, hibp).RegisterAsync(ValidUser(SafeHash));

            Assert.False(result.Success);
            Assert.Equal("Email already registered.", result.Message);
        }

        [Fact]
        public async Task Register_DuplicateEmail_DatabaseCreateNeverCalled()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            await Build(repo, hibp).RegisterAsync(ValidUser(SafeHash));

            repo.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // ── RegisterAsync: database error ────────────────────────────────────────

        [Fact]
        public async Task Register_DatabaseReturnsZero_ReturnsFail()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(0);

            var result = await Build(repo, hibp).RegisterAsync(ValidUser(SafeHash));

            Assert.False(result.Success);
            Assert.Equal("Database error.", result.Message);
        }

        // ── VerifyLoginAsync: happy path ─────────────────────────────────────────

        [Fact]
        public async Task Login_ValidCredentials_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(7)).Returns(Task.CompletedTask);

            var result = await Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key");

            Assert.True(result.Success);
            Assert.Equal("Login successful", result.Message);
        }

        [Fact]
        public async Task Login_ValidCredentials_DataContainsToken()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            var result = await Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key");

            Assert.NotNull(result.Data);
            Assert.False(string.IsNullOrEmpty(result.Data!.Token));
        }

        [Fact]
        public async Task Login_ValidCredentials_DataContainsUserId()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            var result = await Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key");

            Assert.Equal(7, result.Data!.UserId);
        }

        [Fact]
        public async Task Login_ValidCredentials_DataContainsUsername()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            var result = await Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key");

            Assert.Equal("alice", result.Data!.Username);
        }

        [Fact]
        public async Task Login_ValidCredentials_UpdateLastLoginCalledOnce()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            await Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key");

            repo.Verify(r => r.UpdateLastLoginAsync(7), Times.Once);
        }

        // ── VerifyLoginAsync: failure paths ─────────────────────────────────────

        [Fact]
        public async Task Login_UserNotFound_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var result = await Build(repo).VerifyLoginAsync("nobody@example.com", "any-key");

            Assert.False(result.Success);
            Assert.Equal("Invalid email or password.", result.Message);
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            var result = await Build(repo).VerifyLoginAsync("alice@example.com", "wrong-key");

            Assert.False(result.Success);
            Assert.Equal("Invalid email or password.", result.Message);
        }

        [Fact]
        public async Task Login_WrongPassword_SameMessageAsNotFound()
        {
            // The error message must be identical for wrong-password and not-found so
            // an attacker cannot enumerate valid email addresses.
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.GetUserByEmailAsync("nobody@example.com")).ReturnsAsync((User?)null);

            var wrongPw   = await Build(repo).VerifyLoginAsync("alice@example.com", "wrong");
            var notFound  = await Build(repo).VerifyLoginAsync("nobody@example.com", "any");

            Assert.Equal(wrongPw.Message, notFound.Message);
        }

        [Fact]
        public async Task Login_WrongPassword_DoesNotCallUpdateLastLogin()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            await Build(repo).VerifyLoginAsync("alice@example.com", "wrong-key");

            repo.Verify(r => r.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Login_UserNotFound_DoesNotCallUpdateLastLogin()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            await Build(repo).VerifyLoginAsync("nobody@example.com", "any-key");

            repo.Verify(r => r.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
        }

        // ── VerifyLoginAsync: UpdateLastLogin exception propagates ───────────────

        [Fact]
        public async Task Login_UpdateLastLoginThrows_ExceptionPropagates()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(7)).ThrowsAsync(new Exception("DB timeout"));

            await Assert.ThrowsAsync<Exception>(()
                => Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key"));
        }

        // ── GetUserSaltsAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetSalts_UserExists_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            var result = await Build(repo).GetUserSaltsAsync("alice@example.com");

            Assert.True(result.Success);
            Assert.Equal("Salts retrieved successfully", result.Message);
        }

        [Fact]
        public async Task GetSalts_UserExists_DataContainsBothSalts()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            var result = await Build(repo).GetUserSaltsAsync("alice@example.com");

            Assert.NotNull(result.Data);
            Assert.Equal("authsalt==",  result.Data!.AuthSalt);
            Assert.Equal("encsalt==",   result.Data.EncryptionSalt);
        }

        [Fact]
        public async Task GetSalts_UserNotFound_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var result = await Build(repo).GetUserSaltsAsync("nobody@example.com");

            Assert.False(result.Success);
            Assert.Equal("User not found.", result.Message);
        }
    }
}
