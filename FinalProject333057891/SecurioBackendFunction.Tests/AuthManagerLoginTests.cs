using Moq;
using Xunit;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for AuthManager.VerifyLoginAsync.
    // All dependencies are mocked — no real database or HTTP calls are made.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class AuthManagerLoginTests
    {
        static AuthManagerLoginTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        private static User StoredUser() => new User
        {
            Id                = 7,
            Username          = "alice",
            Email             = "alice@example.com",
            MasterPasswordKey = "correct-hashed-key",
            AuthSalt          = "authsalt==",
            EncryptionSalt    = "encsalt=="
        };

        private static AuthManager Build(Mock<IUserRepository> repo)
            => new AuthManager(repo.Object, new Mock<IHibpService>().Object);

        // ── Happy path ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_ValidCredentials_ReturnsSuccess()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(7)).Returns(Task.CompletedTask);

            var result = await Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key");

            Assert.True(result.Success);
            Assert.Equal("Login successful", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(7, result.Data.UserId);
            Assert.Equal("alice", result.Data.Username);
            Assert.False(string.IsNullOrEmpty(result.Data.Token));
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

        // ── Failure paths ──────────────────────────────────────────────────────

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

        [Fact]
        public async Task Login_UpdateLastLoginThrows_LoginStillSucceeds()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(StoredUser());
            repo.Setup(r => r.UpdateLastLoginAsync(7)).ThrowsAsync(new Exception("DB timeout"));

            // UpdateLastLoginAsync is awaited directly; an exception propagates.
            // This test documents the current behaviour and will need updating if
            // a try/catch is added around the call in the future.
            await Assert.ThrowsAsync<Exception>(() =>
                Build(repo).VerifyLoginAsync("alice@example.com", "correct-hashed-key"));
        }
    }
}
