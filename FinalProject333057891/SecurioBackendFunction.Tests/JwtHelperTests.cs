using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;
using SecurioBackendFunction.Helpers;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for JwtHelper — token generation, validation, and claim extraction.
    // The JwtSecret env var must be set before any JwtHelper method is called because
    // the static field is initialised once at first use.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class JwtHelperTests
    {
        static JwtHelperTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        // ── GenerateJwtToken ──────────────────────────────────────────────────────

        [Fact]
        public void GenerateJwtToken_ReturnsNonEmptyString()
        {
            string token = JwtHelper.GenerateJwtToken(1, "alice");
            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public void GenerateJwtToken_ResultIsParseableJwt()
        {
            string token   = JwtHelper.GenerateJwtToken(1, "alice");
            var    handler = new JwtSecurityTokenHandler();

            Assert.True(handler.CanReadToken(token));
        }

        [Fact]
        public void GenerateJwtToken_ContainsUserIdClaim()
        {
            const int userId = 42;
            string token     = JwtHelper.GenerateJwtToken(userId, "alice");
            var handler      = new JwtSecurityTokenHandler();
            var jwt          = handler.ReadJwtToken(token);

            var claim = jwt.Claims.FirstOrDefault(c => c.Type == "user_id");
            Assert.NotNull(claim);
            Assert.Equal(userId.ToString(), claim!.Value);
        }

        [Fact]
        public void GenerateJwtToken_ContainsUsernameClaim()
        {
            string token = JwtHelper.GenerateJwtToken(1, "alice");
            var handler  = new JwtSecurityTokenHandler();
            var jwt      = handler.ReadJwtToken(token);

            var claim = jwt.Claims.FirstOrDefault(c => c.Type == "username");
            Assert.NotNull(claim);
            Assert.Equal("alice", claim!.Value);
        }

        [Fact]
        public void GenerateJwtToken_TwoCallsSameArgs_ReturnDifferentJti()
        {
            // Each token must have a unique JTI to prevent replay attacks.
            string t1 = JwtHelper.GenerateJwtToken(1, "alice");
            string t2 = JwtHelper.GenerateJwtToken(1, "alice");
            var handler = new JwtSecurityTokenHandler();

            var jti1 = handler.ReadJwtToken(t1).Id;
            var jti2 = handler.ReadJwtToken(t2).Id;

            Assert.NotEqual(jti1, jti2);
        }

        [Fact]
        public void GenerateJwtToken_ExpiresInApproximatelyTwoHours()
        {
            string token = JwtHelper.GenerateJwtToken(1, "alice");
            var handler  = new JwtSecurityTokenHandler();
            var jwt      = handler.ReadJwtToken(token);

            var minutesUntilExpiry = (jwt.ValidTo - DateTime.UtcNow).TotalMinutes;
            // Allow a small tolerance around the expected 120-minute lifetime.
            Assert.True(minutesUntilExpiry > 115 && minutesUntilExpiry < 125);
        }

        // ── ValidateToken ─────────────────────────────────────────────────────────

        [Fact]
        public void ValidateToken_ValidToken_ReturnsPrincipal()
        {
            string token = JwtHelper.GenerateJwtToken(1, "alice");
            var principal = JwtHelper.ValidateToken(token);
            Assert.NotNull(principal);
        }

        [Fact]
        public void ValidateToken_ValidToken_PrincipalContainsUserIdClaim()
        {
            const int userId  = 77;
            string token      = JwtHelper.GenerateJwtToken(userId, "bob");
            var principal     = JwtHelper.ValidateToken(token);

            Assert.NotNull(principal);
            var claim = principal!.FindFirst("user_id");
            Assert.NotNull(claim);
            Assert.Equal(userId.ToString(), claim!.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateToken_NullOrEmptyToken_ReturnsNull(string? token)
        {
            Assert.Null(JwtHelper.ValidateToken(token!));
        }

        [Fact]
        public void ValidateToken_TamperedToken_ReturnsNull()
        {
            Assert.Null(JwtHelper.ValidateToken("this.is.not.valid"));
        }

        [Fact]
        public void ValidateToken_TokenSignedWithDifferentSecret_ReturnsNull()
        {
            // Generate a JWT using a different secret (simulates a token from another system).
            const string otherSecret = "completely-different-secret-key!!";
            var key         = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                                  System.Text.Encoding.UTF8.GetBytes(otherSecret));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                                  key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                claims:            new[] { new Claim("user_id", "1") },
                expires:           DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);
            string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            Assert.Null(JwtHelper.ValidateToken(tokenString));
        }

        // ── GetUserIdFromPrincipal ────────────────────────────────────────────────

        [Fact]
        public void GetUserIdFromPrincipal_ValidPrincipal_ExtractsCorrectId()
        {
            const int userId = 42;
            string token     = JwtHelper.GenerateJwtToken(userId, "alice");
            var principal    = JwtHelper.ValidateToken(token)!;

            int extracted = JwtHelper.GetUserIdFromPrincipal(principal);

            Assert.Equal(userId, extracted);
        }

        [Fact]
        public void GetUserIdFromPrincipal_NullPrincipal_ReturnsZero()
        {
            int result = JwtHelper.GetUserIdFromPrincipal(null!);
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetUserIdFromPrincipal_PrincipalWithNoUserIdClaim_ReturnsZero()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("username", "alice")
                // no "user_id" claim
            }));

            int result = JwtHelper.GetUserIdFromPrincipal(principal);
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetUserIdFromPrincipal_UserIdClaimNotANumber_ReturnsZero()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("user_id", "not_a_number")
            }));

            int result = JwtHelper.GetUserIdFromPrincipal(principal);
            Assert.Equal(0, result);
        }
    }
}
