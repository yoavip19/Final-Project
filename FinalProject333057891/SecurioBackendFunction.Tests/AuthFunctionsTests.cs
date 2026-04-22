using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioBackendFunction.ServerFunctions;
using SecurioModels;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests
{
    // Integration-style unit tests for AuthFunctions HTTP endpoints:
    //   Register, Login (VerifyLogin), GetSalts, ValidateToken.
    // Real AuthManager is wired to mocked repository and HIBP service.
    public class AuthFunctionsTests
    {
        static AuthFunctionsTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        private const string SafeHash  = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        private const string PwnedHash = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        private static User ValidUser(string? sha1 = null) => new User
        {
            Username         = "alice",
            Email            = "alice@example.com",
            MasterPasswordKey = "derivedkey==",
            AuthSalt         = "authsalt==",
            EncryptionSalt   = "encsalt==",
            PasswordSha1Hash = sha1
        };

        private static User StoredUser() => new User
        {
            Id               = 7,
            Username         = "alice",
            Email            = "alice@example.com",
            MasterPasswordKey = "correct-hashed-key",
            AuthSalt         = "authsalt==",
            EncryptionSalt   = "encsalt=="
        };

        private static AuthFunctions Build(
            Mock<IUserRepository> repo, Mock<IHibpService> hibp)
            => new AuthFunctions(new AuthManager(repo.Object, hibp.Object));

        // ── Register: happy path ─────────────────────────────────────────────────

        [Fact]
        public async Task Register_ValidUser_Returns200()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            var result = await Build(repo, hibp).Register(HttpTestHelpers.BuildRequest(null, ValidUser(SafeHash)));

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Register_ValidUser_ResponseContainsToken()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(5);

            var result = await Build(repo, hibp).Register(HttpTestHelpers.BuildRequest(null, ValidUser(SafeHash)));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(ok.Value);

            Assert.True(response.Success);
            Assert.False(string.IsNullOrWhiteSpace(response.Data!.Token));
        }

        [Fact]
        public async Task Register_ValidUser_ResponseContainsAssignedUserId()
        {
            const int assignedId = 42;
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(assignedId);

            var result = await Build(repo, hibp).Register(HttpTestHelpers.BuildRequest(null, ValidUser(SafeHash)));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(ok.Value);

            Assert.Equal(assignedId, response.Data!.UserId);
        }

        // ── Register: pwned password ─────────────────────────────────────────────

        [Fact]
        public async Task Register_PwnedPassword_Returns409()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var result = await Build(new Mock<IUserRepository>(), hibp)
                .Register(HttpTestHelpers.BuildRequest(null, ValidUser(PwnedHash)));

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Register_PwnedPassword_ResponseContainsBreachMessage()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var result = await Build(new Mock<IUserRepository>(), hibp)
                .Register(HttpTestHelpers.BuildRequest(null, ValidUser(PwnedHash)));
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(conflict.Value);

            Assert.False(response.Success);
            Assert.Contains("data breach", response.Message);
        }

        [Fact]
        public async Task Register_PwnedPassword_DatabaseNeverCalled()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);
            var repo = new Mock<IUserRepository>();

            await Build(repo, hibp).Register(HttpTestHelpers.BuildRequest(null, ValidUser(PwnedHash)));

            repo.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // ── Register: duplicate email ────────────────────────────────────────────

        [Fact]
        public async Task Register_DuplicateEmail_Returns409()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var result = await Build(repo, hibp).Register(HttpTestHelpers.BuildRequest(null, ValidUser(SafeHash)));

            Assert.IsType<ConflictObjectResult>(result);
        }

        // ── Register: bad input ──────────────────────────────────────────────────

        [Fact]
        public async Task Register_EmptyBody_Returns400()
        {
            var result = await Build(new Mock<IUserRepository>(), new Mock<IHibpService>())
                .Register(HttpTestHelpers.BuildRequest(null, null));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── Login: happy path ────────────────────────────────────────────────────

        [Fact]
        public async Task Login_ValidCredentials_Returns200()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(System.Threading.Tasks.Task.CompletedTask);

            var body = new { Email = "alice@example.com", MasterPasswordKey = "correct-hashed-key" };
            var result = await Build(repo, new Mock<IHibpService>())
                .Login(HttpTestHelpers.BuildRequest(null, body));

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidCredentials_ResponseContainsToken()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(System.Threading.Tasks.Task.CompletedTask);

            var body = new { Email = "alice@example.com", MasterPasswordKey = "correct-hashed-key" };
            var result = await Build(repo, new Mock<IHibpService>())
                .Login(HttpTestHelpers.BuildRequest(null, body));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(ok.Value);

            Assert.True(response.Success);
            Assert.False(string.IsNullOrWhiteSpace(response.Data!.Token));
        }

        // ── Login: failures ──────────────────────────────────────────────────────

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            var body = new { Email = "alice@example.com", MasterPasswordKey = "wrong-key" };
            var result = await Build(repo, new Mock<IHibpService>())
                .Login(HttpTestHelpers.BuildRequest(null, body));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_UserNotFound_Returns401()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            var body = new { Email = "nobody@example.com", MasterPasswordKey = "any-key" };
            var result = await Build(repo, new Mock<IHibpService>())
                .Login(HttpTestHelpers.BuildRequest(null, body));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_EmptyBody_Returns400()
        {
            var result = await Build(new Mock<IUserRepository>(), new Mock<IHibpService>())
                .Login(HttpTestHelpers.BuildRequest(null, null));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── GetSalts ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetSalts_ExistingEmail_Returns200WithSalts()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            var result = await Build(repo, new Mock<IHibpService>())
                .GetSalts(HttpTestHelpers.BuildRequest(null, new { Email = "alice@example.com" }));

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<SaltData>>(ok.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
        }

        [Fact]
        public async Task GetSalts_UnknownEmail_Returns404()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            var result = await Build(repo, new Mock<IHibpService>())
                .GetSalts(HttpTestHelpers.BuildRequest(null, new { Email = "unknown@example.com" }));

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetSalts_EmptyBody_Returns400()
        {
            var result = await Build(new Mock<IUserRepository>(), new Mock<IHibpService>())
                .GetSalts(HttpTestHelpers.BuildRequest(null, null));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── ValidateToken ────────────────────────────────────────────────────────
        // Note: ValidateToken is synchronous — no await is needed.

        [Fact]
        public void ValidateToken_ValidToken_Returns200()
        {
            var result = Build(new Mock<IUserRepository>(), new Mock<IHibpService>())
                .ValidateToken(HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(5), null));

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void ValidateToken_NoAuthorizationHeader_Returns401()
        {
            var result = Build(new Mock<IUserRepository>(), new Mock<IHibpService>())
                .ValidateToken(HttpTestHelpers.BuildRequest(null, null));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void ValidateToken_InvalidToken_Returns401()
        {
            var result = Build(new Mock<IUserRepository>(), new Mock<IHibpService>())
                .ValidateToken(HttpTestHelpers.BuildRequest("Bearer not.a.valid.jwt", null));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void ValidateToken_NonBearerScheme_Returns401()
        {
            var result = Build(new Mock<IUserRepository>(), new Mock<IHibpService>())
                .ValidateToken(HttpTestHelpers.BuildRequest("Basic dXNlcjpwYXNz", null));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void ValidateToken_ValidToken_ResponseIsSuccess()
        {
            const int userId = 77;
            var result = Build(new Mock<IUserRepository>(), new Mock<IHibpService>())
                .ValidateToken(HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(userId), null));

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<object>>(ok.Value);
            Assert.True(response.Success);
        }
    }
}
