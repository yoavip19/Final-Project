using System;
using System.Collections.Generic;
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
    // Integration-style unit tests for UserFunctions HTTP endpoints:
    //   GetProfile, UpdateUser, DeleteUser, GetPasswordHistory.
    public class UserFunctionsTests
    {
        static UserFunctionsTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        private const string SafeHash  = "AABBCCDDEEFF00112233445566778899AABBCCDD";
        private const string PwnedHash = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        private static User ProfileUser(int id = 1) => new User
        {
            Id       = id,
            Username = "alice",
            Email    = "alice@example.com"
        };

        private static User OldUser(int id = 1) => new User
        {
            Id                = id,
            MasterPasswordKey = "oldkey==",
            AuthSalt          = "oldsalt==",
            LastPasswordUpdate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        private static UserFunctions Build(
            Mock<IUserRepository> userRepo,
            Mock<IVaultItemRepository>? vaultRepo = null,
            Mock<IHibpService>? hibp = null)
        {
            if (hibp == null)
            {
                hibp = new Mock<IHibpService>();
                hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);
            }

            userRepo.Setup(r => r.GetUserByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(OldUser());
            userRepo.Setup(r => r.AddPasswordHistoryAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            return new UserFunctions(new UserManager(
                userRepo.Object,
                (vaultRepo ?? new Mock<IVaultItemRepository>()).Object,
                hibp.Object));
        }

        // ── GetProfile: auth checks ───────────────────────────────────────────────

        [Fact]
        public async Task GetProfile_NoAuthHeader_Returns401()
        {
            var result = await Build(new Mock<IUserRepository>())
                .GetProfile(HttpTestHelpers.BuildRequest(null, null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_NonBearerScheme_Returns401()
        {
            var result = await Build(new Mock<IUserRepository>())
                .GetProfile(HttpTestHelpers.BuildRequest("Basic dXNlcjpwYXNz", null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_InvalidToken_Returns401()
        {
            var result = await Build(new Mock<IUserRepository>())
                .GetProfile(HttpTestHelpers.BuildRequest("Bearer not.a.real.jwt", null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── GetProfile: happy path ────────────────────────────────────────────────

        [Fact]
        public async Task GetProfile_ValidToken_Returns200()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(It.IsAny<int>())).ReturnsAsync(ProfileUser(1));

            var result = await Build(repo).GetProfile(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(1), null));

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_ValidToken_ResponseContainsUserData()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(It.IsAny<int>())).ReturnsAsync(ProfileUser(1));

            var result = await Build(repo).GetProfile(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(1), null));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<User>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("alice", response.Data!.Username);
        }

        [Fact]
        public async Task GetProfile_FetchesProfileForTokenUser()
        {
            const int tokenUserId = 5;
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserProfileAsync(tokenUserId)).ReturnsAsync(ProfileUser(tokenUserId));

            await Build(repo).GetProfile(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(tokenUserId), null));

            repo.Verify(r => r.GetUserProfileAsync(tokenUserId), Times.Once);
        }

        // ── UpdateUser: auth checks ───────────────────────────────────────────────

        [Fact]
        public async Task UpdateUser_NoAuthHeader_Returns401()
        {
            var body = new UpdateAccountRequest { Username = "alice", Email = "alice@example.com" };
            var result = await Build(new Mock<IUserRepository>())
                .UpdateUser(HttpTestHelpers.BuildRequest(null, body));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUser_InvalidToken_Returns401()
        {
            var body = new UpdateAccountRequest { Username = "alice", Email = "alice@example.com" };
            var result = await Build(new Mock<IUserRepository>())
                .UpdateUser(HttpTestHelpers.BuildRequest("Bearer garbage.jwt", body));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── UpdateUser: happy path (no password change) ────────────────────────────

        [Fact]
        public async Task UpdateUser_NoPasswordChange_Returns200()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), false)).ReturnsAsync(true);

            var body = new UpdateAccountRequest
            {
                Username        = "alice",
                Email           = "alice@example.com",
                PasswordChanged = false
            };

            var result = await Build(repo).UpdateUser(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(1), body));
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUser_NoPasswordChange_ResponseSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            repo.Setup(r => r.UpdateUserAsync(It.IsAny<User>(), false)).ReturnsAsync(true);

            var body = new UpdateAccountRequest
            {
                Username        = "alice",
                Email           = "alice@example.com",
                PasswordChanged = false
            };

            var result = await Build(repo).UpdateUser(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(1), body));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<object>>(ok.Value);
            Assert.True(response.Success);
        }

        // ── UpdateUser: validation failures ──────────────────────────────────────

        [Fact]
        public async Task UpdateUser_EmptyBody_Returns400()
        {
            var result = await Build(new Mock<IUserRepository>())
                .UpdateUser(HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), null));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUser_DuplicateEmail_Returns400()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            var body = new UpdateAccountRequest { Username = "alice", Email = "taken@example.com" };

            var result = await Build(repo).UpdateUser(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), body));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── UpdateUser: pwned password ────────────────────────────────────────────

        [Fact]
        public async Task UpdateUser_PasswordChanged_PwnedHash_Returns400()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(PwnedHash)).ReturnsAsync(true);

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.EmailExistsForOtherUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            var body = new UpdateAccountRequest
            {
                Username          = "alice",
                Email             = "alice@example.com",
                PasswordChanged   = true,
                MasterPasswordKey = "newkey==",
                AuthSalt          = "newsalt==",
                EncryptionSalt    = "newencsalt==",
                PasswordSha1Hash  = PwnedHash
            };

            var result = await Build(repo, hibp: hibp).UpdateUser(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), body));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── DeleteUser: auth checks ───────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_NoAuthHeader_Returns401()
        {
            var result = await Build(new Mock<IUserRepository>())
                .DeleteUser(HttpTestHelpers.BuildRequest(null, null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task DeleteUser_InvalidToken_Returns401()
        {
            var result = await Build(new Mock<IUserRepository>())
                .DeleteUser(HttpTestHelpers.BuildRequest("Bearer tampered.jwt", null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── DeleteUser: happy path ────────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_ValidToken_Returns200()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.DeleteUserAsync(It.IsAny<int>())).ReturnsAsync(true);

            var result = await Build(repo).DeleteUser(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(1), null));
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ValidToken_ResponseSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.DeleteUserAsync(It.IsAny<int>())).ReturnsAsync(true);

            var result = await Build(repo).DeleteUser(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(1), null));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<object>>(ok.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task DeleteUser_DeletesCorrectUser()
        {
            const int tokenUserId = 9;
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.DeleteUserAsync(tokenUserId)).ReturnsAsync(true);

            await Build(repo).DeleteUser(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(tokenUserId), null));

            repo.Verify(r => r.DeleteUserAsync(tokenUserId), Times.Once);
        }

        // ── GetPasswordHistory: auth checks ──────────────────────────────────────

        [Fact]
        public async Task GetPasswordHistory_NoAuthHeader_Returns401()
        {
            var result = await Build(new Mock<IUserRepository>())
                .GetPasswordHistory(HttpTestHelpers.BuildRequest(null, null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── GetPasswordHistory: happy path ────────────────────────────────────────

        [Fact]
        public async Task GetPasswordHistory_ValidToken_Returns200()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetLastPasswordHistoryAsync(It.IsAny<int>(), 4))
                .ReturnsAsync(new List<MasterPasswordHistory>());

            var result = await Build(repo).GetPasswordHistory(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(1), null));
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetPasswordHistory_ValidToken_ResponseContainsList()
        {
            var history = new List<MasterPasswordHistory>
            {
                new MasterPasswordHistory { Id = 1, UserId = 1, PasswordKey = "key1", AuthSalt = "salt1" }
            };
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetLastPasswordHistoryAsync(It.IsAny<int>(), 4)).ReturnsAsync(history);

            var result = await Build(repo).GetPasswordHistory(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(1), null));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<List<MasterPasswordHistory>>>(ok.Value);
            Assert.Single(response.Data!);
        }

        [Fact]
        public async Task GetPasswordHistory_FetchesHistoryForTokenUser()
        {
            const int tokenUserId = 3;
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetLastPasswordHistoryAsync(tokenUserId, 4))
                .ReturnsAsync(new List<MasterPasswordHistory>());

            await Build(repo).GetPasswordHistory(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(tokenUserId), null));

            repo.Verify(r => r.GetLastPasswordHistoryAsync(tokenUserId, 4), Times.Once);
        }
    }
}
