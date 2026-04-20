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
    // Integration-style tests for the UserFunctions HTTP endpoints.
    // Exercises GetProfile, UpdateUser, and DeleteUser through the full
    // HTTP → Manager → (mocked) Repository chain.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class UserFunctionsTests
    {
        static UserFunctionsTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        private const int TokenUserId = 5;

        // ── Helpers ───────────────────────────────────────────────────────────────

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

        private static string Token() => JwtHelper.GenerateJwtToken(TokenUserId, "testuser");

        private static UserFunctions BuildFunctions(
            Mock<IUserRepository> userRepo,
            Mock<IVaultItemRepository>? vaultRepo = null,
            Mock<IHibpService>? hibp = null)
        {
            hibp ??= new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);
            return new UserFunctions(new UserManager(
                userRepo.Object,
                (vaultRepo ?? new Mock<IVaultItemRepository>()).Object,
                hibp.Object));
        }

        // ── GetProfile: authorization ─────────────────────────────────────────────

        [Fact]
        public async Task GetProfile_NoAuthHeader_Returns401()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .GetProfile(BuildRequest(null, null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_NonBearerScheme_Returns401()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .GetProfile(BuildRequest("Basic dXNlcjpwYXNz", null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_TamperedToken_Returns401()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .GetProfile(BuildRequest("Bearer this.is.not.valid", null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── GetProfile: happy path ────────────────────────────────────────────────

        [Fact]
        public async Task GetProfile_ValidToken_Returns200WithProfile()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(TokenUserId)).ReturnsAsync(new User
            {
                Id       = TokenUserId,
                Username = "testuser",
                Email    = "test@example.com"
            });

            var result   = await BuildFunctions(repo).GetProfile(BuildRequest("Bearer " + Token(), null));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<User>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal(TokenUserId, response.Data!.Id);
        }

        [Fact]
        public async Task GetProfile_UserNotFound_Returns200WithFailResponse()
        {
            // GetProfile wraps the manager result in OkObjectResult regardless of success.
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(TokenUserId)).ReturnsAsync((User?)null);

            var result   = await BuildFunctions(repo).GetProfile(BuildRequest("Bearer " + Token(), null));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<User>>(ok.Value);

            Assert.False(response.Success);
            Assert.Equal("Profile not found.", response.Message);
        }

        // ── UpdateUser: authorization ─────────────────────────────────────────────

        [Fact]
        public async Task UpdateUser_NoAuthHeader_Returns401()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .UpdateUser(BuildRequest(null, new { Username = "u", Email = "e@e.com" }));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUser_TamperedToken_Returns401()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .UpdateUser(BuildRequest("Bearer this.is.not.valid",
                    new { Username = "u", Email = "e@e.com" }));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── UpdateUser: body validation ───────────────────────────────────────────

        [Fact]
        public async Task UpdateUser_NullBody_Returns400()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .UpdateUser(BuildRequest("Bearer " + Token(), null));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── UpdateUser: happy path ────────────────────────────────────────────────

        [Fact]
        public async Task UpdateUser_ValidRequest_Returns200WithSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), false)).ReturnsAsync(true);

            var body = new { Username = "updateduser", Email = "updated@example.com", PasswordChanged = false };
            var result   = await BuildFunctions(repo).UpdateUser(BuildRequest("Bearer " + Token(), body));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<object>>(ok.Value);

            Assert.True(response.Success);
        }

        // ── UpdateUser: validation failure paths ──────────────────────────────────

        [Fact]
        public async Task UpdateUser_DuplicateEmail_Returns400()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);

            var body = new { Username = "updateduser", Email = "taken@example.com", PasswordChanged = false };
            var result = await BuildFunctions(repo).UpdateUser(BuildRequest("Bearer " + Token(), body));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUser_EmptyUsername_Returns400()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);

            var body = new { Username = "", Email = "updated@example.com", PasswordChanged = false };
            var result = await BuildFunctions(repo).UpdateUser(BuildRequest("Bearer " + Token(), body));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── DeleteUser: authorization ─────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_NoAuthHeader_Returns401()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .DeleteUser(BuildRequest(null, null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task DeleteUser_TamperedToken_Returns401()
        {
            var result = await BuildFunctions(new Mock<IUserRepository>())
                .DeleteUser(BuildRequest("Bearer bad.token", null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── DeleteUser: happy path ────────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_ValidToken_UserExists_Returns200WithSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.DeleteUserAsync(TokenUserId)).ReturnsAsync(true);

            var result   = await BuildFunctions(repo).DeleteUser(BuildRequest("Bearer " + Token(), null));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<object>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Account deleted successfully.", response.Message);
        }

        [Fact]
        public async Task DeleteUser_UserNotFound_Returns200WithFailResponse()
        {
            // DeleteUser wraps the manager result in OkObjectResult regardless of success.
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.DeleteUserAsync(TokenUserId)).ReturnsAsync(false);

            var result   = await BuildFunctions(repo).DeleteUser(BuildRequest("Bearer " + Token(), null));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<object>>(ok.Value);

            Assert.False(response.Success);
            Assert.Equal("Account not found.", response.Message);
        }

        // ── UserId binding security ───────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_AlwaysUsesTokenUserId_NotBody()
        {
            // Verifies the correct user is deleted — the one from the JWT, not any
            // attacker-controlled value in the request body.
            int? deletedId = null;
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.DeleteUserAsync(It.IsAny<int>()))
                .Callback<int>(id => deletedId = id)
                .ReturnsAsync(true);

            await BuildFunctions(repo).DeleteUser(BuildRequest("Bearer " + Token(), null));

            Assert.Equal(TokenUserId, deletedId);
        }
    }
}
