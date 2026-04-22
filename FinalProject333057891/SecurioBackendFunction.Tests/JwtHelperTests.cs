using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Xunit;
using SecurioBackendFunction.Helpers;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for JwtHelper: token generation, validation, and claim extraction.
    // All tests set the JwtSecret environment variable required by JwtHelper's static initializer.
    public class JwtHelperTests
    {
        static JwtHelperTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        // ── GenerateJwtToken ─────────────────────────────────────────────────────

        [Fact]
        public void GenerateJwtToken_ReturnsNonEmptyString()
        {
            string token = JwtHelper.GenerateJwtToken(1, "alice");
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void GenerateJwtToken_TokenHasThreeSegments()
        {
            // JWTs are header.payload.signature — exactly 3 dot-separated segments.
            string token = JwtHelper.GenerateJwtToken(1, "alice");
            Assert.Equal(3, token.Split('.').Length);
        }

        [Fact]
        public void GenerateJwtToken_ContainsUserIdClaim()
        {
            string token = JwtHelper.GenerateJwtToken(42, "bob");
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var claim = jwt.Claims.FirstOrDefault(c => c.Type == "user_id");
            Assert.NotNull(claim);
            Assert.Equal("42", claim.Value);
        }

        [Fact]
        public void GenerateJwtToken_ContainsUsernameClaim()
        {
            string token = JwtHelper.GenerateJwtToken(1, "charlie");
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var claim = jwt.Claims.FirstOrDefault(c => c.Type == "username");
            Assert.NotNull(claim);
            Assert.Equal("charlie", claim.Value);
        }

        [Fact]
        public void GenerateJwtToken_TwoCallsSamePrincipal_ProduceDifferentTokens()
        {
            // Each token has a unique JTI, so two tokens for the same user differ.
            string t1 = JwtHelper.GenerateJwtToken(1, "alice");
            string t2 = JwtHelper.GenerateJwtToken(1, "alice");
            Assert.NotEqual(t1, t2);
        }

        [Fact]
        public void GenerateJwtToken_ExpiresInApproximatelyTwoHours()
        {
            string token = JwtHelper.GenerateJwtToken(1, "alice");
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            // Allow a few seconds of tolerance for test execution time.
            var expectedExpiry = DateTime.UtcNow.AddHours(2);
            Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalSeconds) < 10);
        }

        // ── ValidateToken ────────────────────────────────────────────────────────

        [Fact]
        public void ValidateToken_ValidToken_ReturnsPrincipal()
        {
            string token = JwtHelper.GenerateJwtToken(1, "alice");
            var principal = JwtHelper.ValidateToken(token);
            Assert.NotNull(principal);
        }

        [Fact]
        public void ValidateToken_NullToken_ReturnsNull()
        {
            Assert.Null(JwtHelper.ValidateToken(null));
        }

        [Fact]
        public void ValidateToken_EmptyToken_ReturnsNull()
        {
            Assert.Null(JwtHelper.ValidateToken(""));
        }

        [Fact]
        public void ValidateToken_TamperedToken_ReturnsNull()
        {
            string token = JwtHelper.GenerateJwtToken(1, "alice");
            // Flip a character in the signature segment.
            var parts = token.Split('.');
            parts[2] = parts[2].Length > 0
                ? (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..]
                : "tampered";
            string tampered = string.Join(".", parts);
            Assert.Null(JwtHelper.ValidateToken(tampered));
        }

        [Fact]
        public void ValidateToken_RandomString_ReturnsNull()
        {
            Assert.Null(JwtHelper.ValidateToken("this.is.not.a.jwt"));
        }

        [Fact]
        public void ValidateToken_ValidToken_PrincipalContainsUserIdClaim()
        {
            string token = JwtHelper.GenerateJwtToken(99, "alice");
            var principal = JwtHelper.ValidateToken(token);
            Assert.NotNull(principal);
            Assert.NotNull(principal.FindFirst("user_id"));
            Assert.Equal("99", principal.FindFirst("user_id")!.Value);
        }

        // ── GetUserIdFromPrincipal ────────────────────────────────────────────────

        [Fact]
        public void GetUserIdFromPrincipal_ValidPrincipal_ReturnsCorrectId()
        {
            string token = JwtHelper.GenerateJwtToken(77, "alice");
            var principal = JwtHelper.ValidateToken(token);
            Assert.Equal(77, JwtHelper.GetUserIdFromPrincipal(principal));
        }

        [Fact]
        public void GetUserIdFromPrincipal_NullPrincipal_ReturnsZero()
        {
            Assert.Equal(0, JwtHelper.GetUserIdFromPrincipal(null));
        }

        [Fact]
        public void ValidateToken_ThenExtractUserId_RoundTrip()
        {
            const int userId = 123;
            string token = JwtHelper.GenerateJwtToken(userId, "testuser");
            var principal = JwtHelper.ValidateToken(token);
            int extracted = JwtHelper.GetUserIdFromPrincipal(principal);
            Assert.Equal(userId, extracted);
        }
    }
}
