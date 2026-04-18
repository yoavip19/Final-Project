using Moq;
using Xunit;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for AuthManager.RegisterAsync, focusing on the HIBP breach check.
    // All dependencies are mocked — no real database or HTTP calls are made.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class AuthManagerRegisterTests
    {
        static AuthManagerRegisterTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        // Valid SHA-1 hex string used as a stand-in for a non-leaked password hash.
        private const string SafeHash   = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        // Valid SHA-1 hex string used as a stand-in for a leaked password hash.
        private const string PwnedHash  = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        // Returns a valid user for registration tests.
        private static User ValidUser(string? sha1Hash = null) => new User
        {
            Username          = "testuser",
            Email             = "test@example.com",
            MasterPasswordKey = "derivedkey==",
            AuthSalt          = "authsalt==",
            EncryptionSalt    = "encsalt==",
            PasswordSha1Hash  = sha1Hash
        };

        // Builds AuthManager wired to the provided mocks.
        private static AuthManager Build(Mock<IUserRepository> repo, Mock<IHibpService> hibp)
            => new AuthManager(repo.Object, hibp.Object);

        // ── HIBP check – pwned password ──────────────────────────────────────────

        [Fact]
        public async Task Register_PwnedPassword_ReturnsFail()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var repo = new Mock<IUserRepository>();
            var manager = Build(repo, hibp);

            var result = await manager.RegisterAsync(ValidUser(PwnedHash));

            Assert.False(result.Success);
            Assert.Equal("Password has been found in a data breach. Please choose a different password.", result.Message);
        }

        [Fact]
        public async Task Register_PwnedPassword_DoesNotCreateUser()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var repo = new Mock<IUserRepository>();
            var manager = Build(repo, hibp);

            await manager.RegisterAsync(ValidUser(PwnedHash));

            repo.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // ── HIBP check – safe password ───────────────────────────────────────────

        [Fact]
        public async Task Register_SafePassword_ProceedsToEmailCheck()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            var result = await Build(repo, hibp).RegisterAsync(ValidUser(SafeHash));

            Assert.True(result.Success);
            Assert.Equal("User registered successfully", result.Message);
        }

        // ── HIBP check – hash not provided ──────────────────────────────────────

        [Fact]
        public async Task Register_NullHash_SkipsHibpCheck()
        {
            var hibp = new Mock<IHibpService>();

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            await Build(repo, hibp).RegisterAsync(ValidUser(null));

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Register_EmptyHash_SkipsHibpCheck()
        {
            var hibp = new Mock<IHibpService>();

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            await Build(repo, hibp).RegisterAsync(ValidUser("   "));

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        // ── HIBP check is always invoked when a hash is provided ────────────────

        [Fact]
        public async Task Register_HashProvided_HibpServiceCalledExactlyOnce()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            await Build(repo, hibp).RegisterAsync(ValidUser(SafeHash));

            hibp.Verify(h => h.IsPasswordPwnedAsync(SafeHash), Times.Once);
        }

        // ── Duplicate email check still applies ─────────────────────────────────

        [Fact]
        public async Task Register_SafePasswordButDuplicateEmail_ReturnsFail()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var result = await Build(repo, hibp).RegisterAsync(ValidUser(SafeHash));

            Assert.False(result.Success);
            Assert.Equal("Email already registered.", result.Message);
        }
    }
}
