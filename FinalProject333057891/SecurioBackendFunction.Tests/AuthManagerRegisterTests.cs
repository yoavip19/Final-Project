using Moq;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests;

public class AuthManagerRegisterTests
{
    private readonly Mock<IUserRepository> _repoMock;
    private readonly AuthManager _authManager;

    public AuthManagerRegisterTests()
    {
        // Ensure JwtHelper has a secret available during tests
        Environment.SetEnvironmentVariable("JwtSecret", "TestSecretKeyForUnitTests_AtLeast32Chars!");

        _repoMock = new Mock<IUserRepository>();
        _authManager = new AuthManager(_repoMock.Object);
    }

    private static User CreateValidUser() => new User
    {
        Username = "testuser",
        Email = "test@example.com",
        MasterPasswordKey = "derivedKeyBase64==",
        AuthSalt = "authSaltBase64==",
        EncryptionSalt = "encSaltBase64=="
    };

    [Fact]
    public async Task RegisterAsync_ValidNewUser_ReturnsSuccessWithAuthData()
    {
        // Arrange
        var user = CreateValidUser();
        _repoMock.Setup(r => r.EmailExistsAsync(user.Email)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateUserAsync(user)).ReturnsAsync(42);

        // Act
        var result = await _authManager.RegisterAsync(user);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("User registered successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(42, result.Data.UserId);
        Assert.Equal("testuser", result.Data.Username);
        Assert.False(string.IsNullOrEmpty(result.Data.Token));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var user = CreateValidUser();
        _repoMock.Setup(r => r.EmailExistsAsync(user.Email)).ReturnsAsync(true);

        // Act
        var result = await _authManager.RegisterAsync(user);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Email already registered.", result.Message);
        Assert.Null(result.Data);
        _repoMock.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_DatabaseReturnsZeroId_ReturnsFailure()
    {
        // Arrange
        var user = CreateValidUser();
        _repoMock.Setup(r => r.EmailExistsAsync(user.Email)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateUserAsync(user)).ReturnsAsync(0);

        // Act
        var result = await _authManager.RegisterAsync(user);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Database error.", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task RegisterAsync_DatabaseReturnsNegativeId_ReturnsFailure()
    {
        // Arrange
        var user = CreateValidUser();
        _repoMock.Setup(r => r.EmailExistsAsync(user.Email)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateUserAsync(user)).ReturnsAsync(-1);

        // Act
        var result = await _authManager.RegisterAsync(user);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Database error.", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_SuccessfulRegistration_GeneratesJwtToken()
    {
        // Arrange
        var user = CreateValidUser();
        _repoMock.Setup(r => r.EmailExistsAsync(user.Email)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateUserAsync(user)).ReturnsAsync(7);

        // Act
        var result = await _authManager.RegisterAsync(user);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data.Token);
        // JWT tokens have 3 dot-separated parts
        Assert.Equal(3, result.Data.Token.Split('.').Length);
    }

    [Fact]
    public async Task RegisterAsync_CallsEmailExistsBeforeCreate()
    {
        // Arrange
        var user = CreateValidUser();
        var callOrder = new List<string>();
        _repoMock.Setup(r => r.EmailExistsAsync(user.Email))
            .Callback(() => callOrder.Add("EmailExists"))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateUserAsync(user))
            .Callback(() => callOrder.Add("CreateUser"))
            .ReturnsAsync(1);

        // Act
        await _authManager.RegisterAsync(user);

        // Assert
        Assert.Equal(new[] { "EmailExists", "CreateUser" }, callOrder);
    }
}
