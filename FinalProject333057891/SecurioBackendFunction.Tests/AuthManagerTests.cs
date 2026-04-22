using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for AuthManager: RegisterAsync, VerifyLoginAsync, GetUserSaltsAsync.
    public class AuthManagerTests
    {
        static AuthManagerTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        private const string SafeHash  = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        private const string PwnedHash = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        private static User StoredUser() => new User
        {
            Id                = 7,
            Username          = "alice",
            Email             = "alice@example.com",
            MasterPasswordKey = "correct-hashed-key",
            AuthSalt          = "authsalt==",
            EncryptionSalt    = "encsalt=="
        };

        private static User NewUser(string? sha1Hash = null) => new User
        {
            Username          = "newuser",
            Email             = "new@example.com",
            MasterPasswordKey = "derivedkey==",
            AuthSalt          = "authsalt==",
            EncryptionSalt    = "encsalt==",
            PasswordSha1Hash  = sha1Hash
        };

        private static AuthManager Build(Mock<IUserRepository> repo, Mock<IHibpService>? hibp = null)
        {
            hibp ??= new Mock<IHibpService>();
            return new AuthManager(repo.Object, hibp.Object);
        }

        // ── RegisterAsync: happy path ────────────────────────────────────────────

        [Fact]
        public async Task Register_ValidUser_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            var result = await Build(repo).RegisterAsync(NewUser());

            Assert.True(result.Success);
            Assert.Equal("User registered successfully", result.Message);
        }

        [Fact]
        public async Task Register_ValidUser_ResponseDataContainsUserId()
        {
            const int assignedId = 42;
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(assignedId);

            var result = await Build(repo).RegisterAsync(NewUser());

            Assert.NotNull(result.Data);
            Assert.Equal(assignedId, result.Data.UserId);
        }

        [Fact]
        public async Task Register_ValidUser_ResponseDataContainsUsername()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            var result = await Build(repo).RegisterAsync(NewUser());

            Assert.Equal("newuser", result.Data!.Username);
        }

        [Fact]
        public async Task Register_ValidUser_ResponseDataContainsJwtToken()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            var result = await Build(repo).RegisterAsync(NewUser());

            Assert.False(string.IsNullOrWhiteSpace(result.Data!.Token));
        }

        // ── RegisterAsync: HIBP check ────────────────────────────────────────────

        [Fact]
        public async Task Register_PwnedPassword_ReturnsFail()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var result = await Build(new Mock<IUserRepository>(), hibp).RegisterAsync(NewUser(PwnedHash));

            Assert.False(result.Success);
            Assert.Equal(
                "Password has been found in a data breach. Please choose a different password.",
                result.Message);
        }

        [Fact]
        public async Task Register_PwnedPassword_DatabaseNeverCalled()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var repo = new Mock<IUserRepository>();
            await Build(repo, hibp).RegisterAsync(NewUser(PwnedHash));

            repo.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Register_SafePassword_HibpCalledOnce()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            await Build(repo, hibp).RegisterAsync(NewUser(SafeHash));

            hibp.Verify(h => h.IsPasswordPwnedAsync(SafeHash), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Register_NoSha1Hash_HibpSkipped(string? sha1)
        {
            var hibp = new Mock<IHibpService>();
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            await Build(repo, hibp).RegisterAsync(NewUser(sha1));

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        // ── RegisterAsync: duplicate email ───────────────────────────────────────

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var result = await Build(repo).RegisterAsync(NewUser());

            Assert.False(result.Success);
            Assert.Equal("Email already registered.", result.Message);
        }

        [Fact]
        public async Task Register_DuplicateEmail_DatabaseCreateNeverCalled()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            await Build(repo).RegisterAsync(NewUser());

            repo.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // ── RegisterAsync: database error ────────────────────────────────────────

        [Fact]
        public async Task Register_DatabaseReturnsZero_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(0);

            var result = await Build(repo).RegisterAsync(NewUser());

            Assert.False(result.Success);
            Assert.Equal("Database error.", result.Message);
        }

        [Fact]
        public async Task Register_DatabaseReturnsNegative_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(-1);

            var result = await Build(repo).RegisterAsync(NewUser());

            Assert.False(result.Success);
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
        public async Task Login_ValidCredentials_ResponseContainsUserId()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            var result = await Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key");

            Assert.Equal(7, result.Data!.UserId);
        }

        [Fact]
        public async Task Login_ValidCredentials_ResponseContainsUsername()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            var result = await Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key");

            Assert.Equal("alice", result.Data!.Username);
        }

        [Fact]
        public async Task Login_ValidCredentials_ResponseContainsToken()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            var result = await Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key");

            Assert.False(string.IsNullOrWhiteSpace(result.Data!.Token));
        }

        [Fact]
        public async Task Login_ValidCredentials_UpdatesLastLogin()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            await Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key");

            repo.Verify(r => r.UpdateLastLoginAsync(7), Times.Once);
        }

        // ── VerifyLoginAsync: failures ───────────────────────────────────────────

        [Fact]
        public async Task Login_UserNotFound_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);

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
        public async Task Login_WrongPassword_DoesNotUpdateLastLogin()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            await Build(repo).VerifyLoginAsync("alice@example.com", "wrong-key");

            repo.Verify(r => r.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Login_UserNotFound_DoesNotUpdateLastLogin()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            await Build(repo).VerifyLoginAsync("nobody@example.com", "any-key");

            repo.Verify(r => r.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
        }

        // ── GetUserSaltsAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetSalts_ExistingEmail_ReturnsSuccessWithSalts()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            var result = await Build(repo).GetUserSaltsAsync("alice@example.com");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("authsalt==", result.Data.AuthSalt);
            Assert.Equal("encsalt==", result.Data.EncryptionSalt);
        }

        [Fact]
        public async Task GetSalts_UnknownEmail_ReturnsFail()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            var result = await Build(repo).GetUserSaltsAsync("unknown@example.com");

            Assert.False(result.Success);
            Assert.Equal("User not found.", result.Message);
        }

        [Fact]
        public async Task GetSalts_ExistingEmail_MessageIndicatesSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            var result = await Build(repo).GetUserSaltsAsync("alice@example.com");

            Assert.Equal("Salts retrieved successfully", result.Message);
        }
    }
}
