using System;
using System.Threading.Tasks;
using Moq;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests;

/// <summary>
/// Tests for AuthManager: registration and login logic.
/// </summary>
public class AuthManagerTests : IDisposable
{
    private const string TestSecret = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123!";
    private readonly Mock<IUserRepository> _repoMock;
    private readonly AuthManager _manager;

    public AuthManagerTests()
    {
        Environment.SetEnvironmentVariable("JwtSecret", TestSecret);
        _repoMock = new Mock<IUserRepository>();
        _manager = new AuthManager(_repoMock.Object);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("JwtSecret", null);
    }

    // ── RegisterAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithNewUser_ReturnsSuccess()
    {
        var user = MakeUser("alice", "alice@test.com");
        _repoMock.Setup(r => r.EmailExistsAsync("alice@test.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

        var result = await _manager.RegisterAsync(user);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.UserId);
        Assert.Equal("alice", result.Data.Username);
        Assert.False(string.IsNullOrEmpty(result.Data.Token));
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsFailure()
    {
        var user = MakeUser("bob", "bob@test.com");
        _repoMock.Setup(r => r.EmailExistsAsync("bob@test.com")).ReturnsAsync(true);

        var result = await _manager.RegisterAsync(user);

        Assert.False(result.Success);
        Assert.Contains("already registered", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task RegisterAsync_WhenDatabaseReturnsZero_ReturnsFailure()
    {
        var user = MakeUser("charlie", "charlie@test.com");
        _repoMock.Setup(r => r.EmailExistsAsync("charlie@test.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(0);

        var result = await _manager.RegisterAsync(user);

        Assert.False(result.Success);
        Assert.Contains("database error", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_WhenDatabaseReturnsNegative_ReturnsFailure()
    {
        var user = MakeUser("dave", "dave@test.com");
        _repoMock.Setup(r => r.EmailExistsAsync("dave@test.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(-1);

        var result = await _manager.RegisterAsync(user);

        Assert.False(result.Success);
    }

    // ── VerifyLoginAsync ──────────────────────────────────────────

    [Fact]
    public async Task VerifyLoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        var storedUser = new User { Id = 10, Username = "alice", MasterPasswordKey = "hashed_key" };
        _repoMock.Setup(r => r.GetUserByEmailAsync("alice@test.com")).ReturnsAsync(storedUser);

        var result = await _manager.VerifyLoginAsync("alice@test.com", "hashed_key");

        Assert.True(result.Success);
        Assert.Equal(10, result.Data.UserId);
        Assert.Equal("alice", result.Data.Username);
        Assert.False(string.IsNullOrEmpty(result.Data.Token));
    }

    [Fact]
    public async Task VerifyLoginAsync_WithWrongPassword_ReturnsFailure()
    {
        var storedUser = new User { Id = 10, Username = "alice", MasterPasswordKey = "correct_key" };
        _repoMock.Setup(r => r.GetUserByEmailAsync("alice@test.com")).ReturnsAsync(storedUser);

        var result = await _manager.VerifyLoginAsync("alice@test.com", "wrong_key");

        Assert.False(result.Success);
        Assert.Contains("invalid", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyLoginAsync_WithNonExistentEmail_ReturnsFailure()
    {
        _repoMock.Setup(r => r.GetUserByEmailAsync("nobody@test.com")).ReturnsAsync((User)null);

        var result = await _manager.VerifyLoginAsync("nobody@test.com", "any_key");

        Assert.False(result.Success);
        Assert.Contains("invalid", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyLoginAsync_ReturnsTokenThatPassesJwtValidation()
    {
        var storedUser = new User { Id = 5, Username = "eve", MasterPasswordKey = "key123" };
        _repoMock.Setup(r => r.GetUserByEmailAsync("eve@test.com")).ReturnsAsync(storedUser);

        var result = await _manager.VerifyLoginAsync("eve@test.com", "key123");

        Assert.True(result.Success);
        var principal = Helpers.JwtHelper.ValidateToken(result.Data.Token);
        Assert.NotNull(principal);
        Assert.Equal(5, Helpers.JwtHelper.GetUserIdFromPrincipal(principal));
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static User MakeUser(string username, string email) => new()
    {
        Username = username,
        Email = email,
        MasterPasswordKey = "hashed_master",
        AuthSalt = "auth_salt_value",
        EncryptionSalt = "enc_salt_value"
    };
}
