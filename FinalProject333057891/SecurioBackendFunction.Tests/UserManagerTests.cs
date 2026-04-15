using System;
using System.Threading.Tasks;
using Moq;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests;

/// <summary>
/// Tests for UserManager: profile retrieval.
/// </summary>
public class UserManagerTests
{
    private readonly Mock<IUserRepository> _repoMock;
    private readonly UserManager _manager;

    public UserManagerTests()
    {
        _repoMock = new Mock<IUserRepository>();
        _manager = new UserManager(_repoMock.Object);
    }

    [Fact]
    public async Task GetProfileAsync_WithExistingUser_ReturnsSuccess()
    {
        var user = new User
        {
            Id = 1,
            Username = "alice",
            Email = "alice@test.com",
            CreatedAt = new DateTime(2024, 1, 1),
            LastLogin = new DateTime(2024, 6, 1),
            LastPasswordUpdate = new DateTime(2024, 3, 1),
            PasswordCount = 5
        };
        _repoMock.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(user);

        var result = await _manager.GetProfileAsync(1);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("alice", result.Data.Username);
        Assert.Equal("alice@test.com", result.Data.Email);
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentUser_ReturnsFailure()
    {
        _repoMock.Setup(r => r.GetUserProfileAsync(999)).ReturnsAsync((User)null);

        var result = await _manager.GetProfileAsync(999);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsCorrectData()
    {
        var expectedDate = new DateTime(2024, 5, 15);
        var user = new User
        {
            Id = 42,
            Username = "bob",
            Email = "bob@test.com",
            CreatedAt = expectedDate,
            LastLogin = expectedDate,
            LastPasswordUpdate = expectedDate,
            PasswordCount = 10
        };
        _repoMock.Setup(r => r.GetUserProfileAsync(42)).ReturnsAsync(user);

        var result = await _manager.GetProfileAsync(42);

        Assert.True(result.Success);
        Assert.Equal(42, result.Data.Id);
        Assert.Equal(expectedDate, result.Data.CreatedAt);
        Assert.Equal(10, result.Data.PasswordCount);
    }
}
