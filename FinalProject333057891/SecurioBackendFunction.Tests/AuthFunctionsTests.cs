using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioBackendFunction.ServerFunctions;
using SecurioModels;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests;

/// <summary>
/// Tests for AuthFunctions: Register and Login endpoints, including error handling.
/// </summary>
public class AuthFunctionsTests : IDisposable
{
    private const string TestSecret = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123!";
    private readonly Mock<IUserRepository> _repoMock;
    private readonly AuthFunctions _functions;

    public AuthFunctionsTests()
    {
        Environment.SetEnvironmentVariable("JwtSecret", TestSecret);
        _repoMock = new Mock<IUserRepository>();
        var authManager = new AuthManager(_repoMock.Object);
        _functions = new AuthFunctions(authManager);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("JwtSecret", null);
    }

    // ── Register ──────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidNewUser_ReturnsOk()
    {
        _repoMock.Setup(r => r.EmailExistsAsync("alice@test.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

        var req = CreatePostRequest(new User
        {
            Username = "alice",
            Email = "alice@test.com",
            MasterPasswordKey = "key",
            AuthSalt = "asalt",
            EncryptionSalt = "esalt"
        });

        var result = await _functions.Register(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = ok.Value as ServerResponse<AuthData>;
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Equal("alice", body.Data.Username);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        _repoMock.Setup(r => r.EmailExistsAsync("bob@test.com")).ReturnsAsync(true);

        var req = CreatePostRequest(new User
        {
            Username = "bob",
            Email = "bob@test.com",
            MasterPasswordKey = "key",
            AuthSalt = "asalt",
            EncryptionSalt = "esalt"
        });

        var result = await _functions.Register(req);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var body = conflict.Value as ServerResponse<AuthData>;
        Assert.NotNull(body);
        Assert.False(body.Success);
    }

    [Fact]
    public async Task Register_WithInvalidBody_ReturnsBadRequest()
    {
        var req = CreatePostRequest("this is not valid json {{{}}}");

        var result = await _functions.Register(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_WithEmptyBody_ReturnsBadRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Array.Empty<byte>());

        var result = await _functions.Register(context.Request);

        // Empty body causes JsonConvert.DeserializeObject to return null,
        // then accessing .Email on null throws → caught → BadRequest
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── Login ─────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var storedUser = new User { Id = 10, Username = "charlie", MasterPasswordKey = "hashed" };
        _repoMock.Setup(r => r.GetUserByEmailAsync("charlie@test.com")).ReturnsAsync(storedUser);

        var req = CreatePostRequest(new User { Email = "charlie@test.com", MasterPasswordKey = "hashed" });

        var result = await _functions.Login(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = ok.Value as ServerResponse<AuthData>;
        Assert.NotNull(body);
        Assert.True(body.Success);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var storedUser = new User { Id = 10, Username = "charlie", MasterPasswordKey = "correct" };
        _repoMock.Setup(r => r.GetUserByEmailAsync("charlie@test.com")).ReturnsAsync(storedUser);

        var req = CreatePostRequest(new User { Email = "charlie@test.com", MasterPasswordKey = "wrong" });

        var result = await _functions.Login(req);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        _repoMock.Setup(r => r.GetUserByEmailAsync("nobody@test.com")).ReturnsAsync((User)null);

        var req = CreatePostRequest(new User { Email = "nobody@test.com", MasterPasswordKey = "key" });

        var result = await _functions.Login(req);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithInvalidBody_ReturnsBadRequest()
    {
        var req = CreatePostRequest("}{broken json");

        var result = await _functions.Login(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithEmptyBody_ReturnsBadRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Array.Empty<byte>());

        var result = await _functions.Login(context.Request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static HttpRequest CreatePostRequest(object body)
    {
        var json = body is string s ? s : JsonConvert.SerializeObject(body);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return context.Request;
    }
}
