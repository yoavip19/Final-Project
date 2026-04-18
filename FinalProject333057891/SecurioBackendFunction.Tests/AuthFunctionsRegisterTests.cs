using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    // Integration-style unit tests for the AuthFunctions.Register HTTP endpoint.
    // Uses a real AuthManager wired to mocked IUserRepository and IHibpService so
    // the full HTTP → Manager chain is exercised without a real database or network.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class AuthFunctionsRegisterTests
    {
        static AuthFunctionsRegisterTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        private const string SafeHash  = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        private const string PwnedHash = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        // Returns a fully-populated valid user payload.
        private static User ValidUser(string? sha1Hash = null) => new User
        {
            Username          = "alice",
            Email             = "alice@example.com",
            MasterPasswordKey = "derivedkey==",
            AuthSalt          = "authsalt==",
            EncryptionSalt    = "encsalt==",
            PasswordSha1Hash  = sha1Hash
        };

        // Builds a mock HttpRequest with the given object serialized as the body.
        private static HttpRequest BuildRequest(object? body)
        {
            var mock = new Mock<HttpRequest>();
            mock.Setup(r => r.Headers).Returns(new HeaderDictionary());
            var json   = body != null ? JsonConvert.SerializeObject(body) : string.Empty;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            mock.Setup(r => r.Body).Returns(stream);
            return mock.Object;
        }

        // Builds the endpoint under test wired to the supplied repository and HIBP mocks.
        private static AuthFunctions BuildFunctions(
            Mock<IUserRepository> repo, Mock<IHibpService> hibp)
            => new AuthFunctions(new AuthManager(repo.Object, hibp.Object));

        // ── Happy path ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Register_ValidUser_SafePassword_Returns200()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

            var result = await BuildFunctions(repo, hibp).Register(BuildRequest(ValidUser(SafeHash)));

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Register_ValidUser_ResponseBodyContainsToken()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(5);

            var result   = await BuildFunctions(repo, hibp).Register(BuildRequest(ValidUser(SafeHash)));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(ok.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data?.Token);
        }

        [Fact]
        public async Task Register_ValidUser_ResponseBodyContainsAssignedUserId()
        {
            const int assignedId = 42;
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(assignedId);

            var result   = await BuildFunctions(repo, hibp).Register(BuildRequest(ValidUser(SafeHash)));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(ok.Value);

            Assert.Equal(assignedId, response.Data.UserId);
        }

        // ── Pwned password ────────────────────────────────────────────────────────

        [Fact]
        public async Task Register_PwnedPassword_Returns409Conflict()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var result = await BuildFunctions(new Mock<IUserRepository>(), hibp)
                .Register(BuildRequest(ValidUser(PwnedHash)));

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Register_PwnedPassword_ResponseContainsBreachMessage()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var result   = await BuildFunctions(new Mock<IUserRepository>(), hibp)
                .Register(BuildRequest(ValidUser(PwnedHash)));
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(conflict.Value);

            Assert.False(response.Success);
            Assert.Equal(
                "Password has been found in a data breach. Please choose a different password.",
                response.Message);
        }

        [Fact]
        public async Task Register_PwnedPassword_DatabaseNeverCalled()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var repo = new Mock<IUserRepository>();
            await BuildFunctions(repo, hibp).Register(BuildRequest(ValidUser(PwnedHash)));

            repo.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // ── Duplicate email ───────────────────────────────────────────────────────

        [Fact]
        public async Task Register_DuplicateEmail_Returns409Conflict()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var result = await BuildFunctions(repo, hibp).Register(BuildRequest(ValidUser(SafeHash)));

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ResponseContainsEmailMessage()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(SafeHash)).ReturnsAsync(false);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var result   = await BuildFunctions(repo, hibp).Register(BuildRequest(ValidUser(SafeHash)));
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ServerResponse<AuthData>>(conflict.Value);

            Assert.False(response.Success);
            Assert.Equal("Email already registered.", response.Message);
        }

        // ── Bad input ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Register_EmptyBody_Returns400()
        {
            // An empty body deserializes to null; RegisterAsync will throw a NullReferenceException
            // which the function's catch block must convert to a 400 response.
            var result = await BuildFunctions(new Mock<IUserRepository>(), new Mock<IHibpService>())
                .Register(BuildRequest(null));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_EmptyBody_ResponseBodyContainsErrorMessage()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>(), new Mock<IHibpService>())
                .Register(BuildRequest(null));

            var bad      = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<object>>(bad.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Message);
        }
    }
}
