using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SecurioBackendFunction.Helpers;

namespace SecurioBackendFunction.Tests;

/// <summary>
/// Tests for JwtHelper: token generation, validation, expiry, tampering, and claim extraction.
/// </summary>
public class JwtHelperTests : IDisposable
{
    private const string TestSecret = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123!";

    public JwtHelperTests()
    {
        // JwtHelper reads from this env var on first access.
        Environment.SetEnvironmentVariable("JwtSecret", TestSecret);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("JwtSecret", null);
    }

    // ── GenerateJwtToken ──────────────────────────────────────────

    [Fact]
    public void GenerateJwtToken_ReturnsNonEmptyString()
    {
        var token = JwtHelper.GenerateJwtToken(1, "alice");

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateJwtToken_ContainsExpectedClaims()
    {
        var token = JwtHelper.GenerateJwtToken(42, "bob");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("42", jwt.Claims.First(c => c.Type == "user_id").Value);
        Assert.Equal("bob", jwt.Claims.First(c => c.Type == "username").Value);
        Assert.NotNull(jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti));
    }

    [Fact]
    public void GenerateJwtToken_HasFutureExpiry()
    {
        var token = JwtHelper.GenerateJwtToken(1, "alice");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    // ── ValidateToken ─────────────────────────────────────────────

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsClaimsPrincipal()
    {
        var token = JwtHelper.GenerateJwtToken(7, "charlie");

        var principal = JwtHelper.ValidateToken(token);

        Assert.NotNull(principal);
    }

    [Fact]
    public void ValidateToken_WithNull_ReturnsNull()
    {
        Assert.Null(JwtHelper.ValidateToken(null));
    }

    [Fact]
    public void ValidateToken_WithEmptyString_ReturnsNull()
    {
        Assert.Null(JwtHelper.ValidateToken(""));
    }

    [Fact]
    public void ValidateToken_WithGarbageString_ReturnsNull()
    {
        Assert.Null(JwtHelper.ValidateToken("not.a.jwt"));
    }

    [Fact]
    public void ValidateToken_WithTamperedPayload_ReturnsNull()
    {
        var token = JwtHelper.GenerateJwtToken(1, "alice");

        // Tamper with the payload section (second part)
        var parts = token.Split('.');
        var payloadBytes = Convert.FromBase64String(PadBase64(parts[1]));
        payloadBytes[0] ^= 0xFF; // flip a byte
        parts[1] = Convert.ToBase64String(payloadBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tampered = string.Join(".", parts);

        Assert.Null(JwtHelper.ValidateToken(tampered));
    }

    [Fact]
    public void ValidateToken_WithWrongSigningKey_ReturnsNull()
    {
        // Generate a token signed with a different key
        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("DifferentSecretKeyThatIsLongEnough123456!"));
        var creds = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            claims: new[] { new Claim("user_id", "1") },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        Assert.Null(JwtHelper.ValidateToken(token));
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ReturnsNull()
    {
        // Create a token that is already expired using the correct key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            claims: new[] { new Claim("user_id", "99") },
            expires: DateTime.UtcNow.AddSeconds(-1),
            signingCredentials: creds);
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        Assert.Null(JwtHelper.ValidateToken(token));
    }

    // ── GetUserIdFromPrincipal ────────────────────────────────────

    [Fact]
    public void GetUserIdFromPrincipal_WithValidPrincipal_ReturnsUserId()
    {
        var token = JwtHelper.GenerateJwtToken(55, "dave");
        var principal = JwtHelper.ValidateToken(token);

        var userId = JwtHelper.GetUserIdFromPrincipal(principal);

        Assert.Equal(55, userId);
    }

    [Fact]
    public void GetUserIdFromPrincipal_WithNullPrincipal_ReturnsZero()
    {
        Assert.Equal(0, JwtHelper.GetUserIdFromPrincipal(null));
    }

    [Fact]
    public void GetUserIdFromPrincipal_WithMissingClaim_ReturnsZero()
    {
        // A principal with no user_id claim
        var identity = new ClaimsIdentity(new[] { new Claim("other", "value") });
        var principal = new ClaimsPrincipal(identity);

        Assert.Equal(0, JwtHelper.GetUserIdFromPrincipal(principal));
    }

    [Fact]
    public void GetUserIdFromPrincipal_WithNonNumericClaim_ReturnsZero()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("user_id", "not_a_number") });
        var principal = new ClaimsPrincipal(identity);

        Assert.Equal(0, JwtHelper.GetUserIdFromPrincipal(principal));
    }

    // ── Round-trip: generate → validate → extract ─────────────────

    [Theory]
    [InlineData(1, "alice")]
    [InlineData(999, "zara")]
    [InlineData(int.MaxValue, "max_user")]
    public void RoundTrip_GenerateValidateExtract_ReturnsOriginalUserId(int userId, string username)
    {
        var token = JwtHelper.GenerateJwtToken(userId, username);
        var principal = JwtHelper.ValidateToken(token);
        var extracted = JwtHelper.GetUserIdFromPrincipal(principal);

        Assert.Equal(userId, extracted);
    }

    // Helper to pad Base64Url to standard Base64
    private static string PadBase64(string base64Url)
    {
        var s = base64Url.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return s;
    }
}
