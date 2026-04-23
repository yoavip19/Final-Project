using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Newtonsoft.Json;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioBackendFunction.ServerFunctions;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using Xunit;

namespace SecurioBackendFunction.Tests
{
    // Integration-style tests for the AuthFunctions HTTP endpoints.
    // Exercises the full HTTP → Manager → (mocked) Repository chain for:
    //   Register, Login, GetSalts, ValidateToken
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class AuthFunctionsTests
    {
        static AuthFunctionsTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        private const string SafeHash  = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        private const string PwnedHash = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        private static HttpRequest BuildRequest(string? authHeader, object? body)
        {
            var mock    = new Mock<HttpRequest>();
            var headers = new HeaderDictionary();
            if (authHeader != null)
                headers.Add("Authorization", new StringValues(authHeader));
            mock.Setup(r => r.Headers).Returns(headers);

            var json   = body != null ? JsonConvert.SerializeObject(body) : string.Empty;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            mock.Setup(r => r.Body).Returns(stream);
            return mock.Object;
        }

        private static HttpRequest BuildRequest(object? body) => BuildRequest(null, body);

        private static AuthFunctions BuildFunctions(Mock<IUserRepository> repo, Mock<IHibpService>? hibp = null)
        {
            hibp ??= new Mock<IHibpService>();
            return new AuthFunctions(new AuthManager(repo.Object, hibp.Object));
        }

        private static User ValidRegistrationUser(string? sha1Hash = null) => new User
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

        // ── Register ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Register_ValidUser_Returns200()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            var result = await BuildFunctions(repo, hibp).Register(BuildRequest(ValidRegistrationUser(SafeHash)));

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Register_ValidUser_ResponseContainsToken()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            var result   = await BuildFunctions(repo, hibp).Register(BuildRequest(ValidRegistrationUser(SafeHash)));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(ok.Value);

            Assert.True(response.Success);
            Assert.False(string.IsNullOrEmpty(response.Data?.Token));
        }

        [Fact]
        public async Task Register_ValidUser_ResponseContainsAssignedId()
        {
            const int assignedId = 42;
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(assignedId);

            var result   = await BuildFunctions(repo, hibp).Register(BuildRequest(ValidRegistrationUser(SafeHash)));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(ok.Value);

            Assert.Equal(assignedId, response.Data!.UserId);
        }

        [Fact]
        public async Task Register_PwnedPassword_Returns409()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var result = await BuildFunctions(new Mock<IUserRepository>(), hibp)
                .Register(BuildRequest(ValidRegistrationUser(PwnedHash)));

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Register_PwnedPassword_ResponseContainsBreachMessage()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var result   = await BuildFunctions(new Mock<IUserRepository>(), hibp)
                .Register(BuildRequest(ValidRegistrationUser(PwnedHash)));
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(conflict.Value);

            Assert.False(response.Success);
            Assert.Contains("data breach", response.Message);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns409()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var result = await BuildFunctions(repo, hibp).Register(BuildRequest(ValidRegistrationUser(SafeHash)));

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ResponseContainsEmailMessage()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var result   = await BuildFunctions(repo, hibp).Register(BuildRequest(ValidRegistrationUser(SafeHash)));
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(conflict.Value);

            Assert.False(response.Success);
            Assert.Equal("Email already registered.", response.Message);
        }

        [Fact]
        public async Task Register_EmptyBody_Returns400()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .Register(BuildRequest(null));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_EmptyBody_ResponseBodyContainsErrorMessage()
        {
            var result  = await BuildFunctions(new Mock<IUserRepository>())
                .Register(BuildRequest(null));
            var bad     = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<object>>(bad.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Message);
        }

        // ── Login ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_ValidCredentials_Returns200()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            var loginUser = new User { Email = "alice@example.com", MasterPasswordKey = "correct-hashed-key" };
            var result = await BuildFunctions(repo).Login(BuildRequest(loginUser));

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidCredentials_ResponseContainsToken()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            var loginUser = new User { Email = "alice@example.com", MasterPasswordKey = "correct-hashed-key" };
            var result   = await BuildFunctions(repo).Login(BuildRequest(loginUser));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(ok.Value);

            Assert.True(response.Success);
            Assert.False(string.IsNullOrEmpty(response.Data?.Token));
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            var loginUser = new User { Email = "alice@example.com", MasterPasswordKey = "wrong-key" };
            var result = await BuildFunctions(repo).Login(BuildRequest(loginUser));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_UserNotFound_Returns401()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var loginUser = new User { Email = "nobody@example.com", MasterPasswordKey = "any" };
            var result = await BuildFunctions(repo).Login(BuildRequest(loginUser));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_EmptyBody_Returns400()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .Login(BuildRequest(null));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── GetSalts ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetSalts_ValidEmail_Returns200WithSalts()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());

            var result = await BuildFunctions(repo).GetSalts(
                BuildRequest(new { Email = "alice@example.com" }));

            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<SaltData>>(ok.Value);
            Assert.True(response.Success);
            Assert.Equal("authsalt==",  response.Data!.AuthSalt);
            Assert.Equal("encsalt==",   response.Data.EncryptionSalt);
        }

        [Fact]
        public async Task GetSalts_UnknownEmail_Returns404()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var result = await BuildFunctions(repo).GetSalts(
                BuildRequest(new { Email = "nobody@example.com" }));

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetSalts_EmptyBody_Returns400()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .GetSalts(BuildRequest(null));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── ValidateToken ─────────────────────────────────────────────────────────

        [Fact]
        public async Task ValidateToken_ValidToken_Returns200()
        {
            string token = JwtHelper.GenerateJwtToken(1, "testuser");
            var result = await Task.Run(() =>
                BuildFunctions(new Mock<IUserRepository>())
                    .ValidateToken(BuildRequest("Bearer " + token, null)));

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ValidateToken_ValidToken_ResponseContainsUserId()
        {
            const int userId = 42;
            string token = JwtHelper.GenerateJwtToken(userId, "testuser");
            var result = await Task.Run(() =>
                BuildFunctions(new Mock<IUserRepository>())
                    .ValidateToken(BuildRequest("Bearer " + token, null)));

            var ok = Assert.IsType<OkObjectResult>(result);
            // The response Data object carries UserId
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task ValidateToken_NoAuthHeader_Returns401()
        {
            var result = await Task.Run(() =>
                BuildFunctions(new Mock<IUserRepository>())
                    .ValidateToken(BuildRequest(null, null)));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task ValidateToken_NonBearerScheme_Returns401()
        {
            var result = await Task.Run(() =>
                BuildFunctions(new Mock<IUserRepository>())
                    .ValidateToken(BuildRequest("Basic dXNlcjpwYXNz", null)));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task ValidateToken_TamperedToken_Returns401()
        {
            var result = await Task.Run(() =>
                BuildFunctions(new Mock<IUserRepository>())
                    .ValidateToken(BuildRequest("Bearer this.is.not.valid", null)));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}
