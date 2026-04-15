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

namespace SecurioBackendFunction.Tests;

/// <summary>
/// Tests for UserFunctions.GetProfile: JWT validation, missing headers, and successful retrieval.
/// </summary>
public class UserFunctionsTests : IDisposable
{
    private const string TestSecret = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123!";
    private readonly Mock<IUserRepository> _repoMock;
    private readonly UserFunctions _functions;

    public UserFunctionsTests()
    {
        Environment.SetEnvironmentVariable("JwtSecret", TestSecret);
        _repoMock = new Mock<IUserRepository>();
        var userManager = new UserManager(_repoMock.Object);
        _functions = new UserFunctions(userManager);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("JwtSecret", null);
    }

    // ── Happy path ────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_WithValidJwt_ReturnsOkWithProfile()
    {
        var profile = new User { Id = 7, Username = "alice", Email = "alice@test.com" };
        _repoMock.Setup(r => r.GetUserProfileAsync(7)).ReturnsAsync(profile);
        var token = JwtHelper.GenerateJwtToken(7, "alice");
        var req = CreateGetRequest(bearerToken: token);

        var actionResult = await _functions.GetProfile(req);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = ok.Value as ServerResponse<User>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("alice", response.Data.Username);
    }

    // ── Missing / malformed Authorization header ──────────────────

    [Fact]
    public async Task GetProfile_WithNoAuthHeader_ReturnsUnauthorized()
    {
        var req = CreateGetRequest(bearerToken: null);

        var actionResult = await _functions.GetProfile(req);

        var unauth = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var response = unauth.Value as ServerResponse<User>;
        Assert.NotNull(response);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetProfile_WithEmptyAuthHeader_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Headers["Authorization"] = "";

        var actionResult = await _functions.GetProfile(context.Request);

        Assert.IsType<UnauthorizedObjectResult>(actionResult);
    }

    [Fact]
    public async Task GetProfile_WithNonBearerScheme_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Headers["Authorization"] = "Basic dXNlcjpwYXNz";

        var actionResult = await _functions.GetProfile(context.Request);

        Assert.IsType<UnauthorizedObjectResult>(actionResult);
    }

    // ── Invalid JWT tokens ────────────────────────────────────────

    [Fact]
    public async Task GetProfile_WithGarbageToken_ReturnsUnauthorized()
    {
        var req = CreateGetRequest(bearerToken: null);
        req.Headers["Authorization"] = "Bearer not.a.real.token";

        var actionResult = await _functions.GetProfile(req);

        Assert.IsType<UnauthorizedObjectResult>(actionResult);
    }

    [Fact]
    public async Task GetProfile_WithExpiredToken_ReturnsUnauthorized()
    {
        // Create a token that already expired
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: new[] { new System.Security.Claims.Claim("user_id", "7") },
            expires: DateTime.UtcNow.AddSeconds(-1),
            signingCredentials: creds);
        var expiredToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwt);

        var req = CreateGetRequest(bearerToken: expiredToken);

        var actionResult = await _functions.GetProfile(req);

        Assert.IsType<UnauthorizedObjectResult>(actionResult);
    }

    [Fact]
    public async Task GetProfile_WithTokenMissingUserIdClaim_ReturnsUnauthorized()
    {
        // Token signed with correct key but no user_id claim
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: new[] { new System.Security.Claims.Claim("username", "alice") },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwt);

        var req = CreateGetRequest(bearerToken: token);

        var actionResult = await _functions.GetProfile(req);

        // GetUserIdFromPrincipal returns 0, which is <= 0
        Assert.IsType<UnauthorizedObjectResult>(actionResult);
    }

    [Fact]
    public async Task GetProfile_WithTokenSignedByWrongKey_ReturnsUnauthorized()
    {
        var wrongKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("ACompletelyDifferentSecretKeyForAttacker123456!"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(wrongKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: new[] { new System.Security.Claims.Claim("user_id", "7") },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwt);

        var req = CreateGetRequest(bearerToken: token);

        var actionResult = await _functions.GetProfile(req);

        Assert.IsType<UnauthorizedObjectResult>(actionResult);
    }

    // ── Profile not found ─────────────────────────────────────────

    [Fact]
    public async Task GetProfile_WhenProfileDoesNotExist_ReturnsOkWithFailure()
    {
        _repoMock.Setup(r => r.GetUserProfileAsync(7)).ReturnsAsync((User)null);
        var token = JwtHelper.GenerateJwtToken(7, "alice");
        var req = CreateGetRequest(bearerToken: token);

        var actionResult = await _functions.GetProfile(req);

        // The controller returns OkObjectResult wrapping a response with Success = false
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = ok.Value as ServerResponse<User>;
        Assert.NotNull(response);
        Assert.False(response.Success);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static HttpRequest CreateGetRequest(string? bearerToken)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";

        if (bearerToken != null)
        {
            context.Request.Headers["Authorization"] = $"Bearer {bearerToken}";
        }

        return context.Request;
    }
}
