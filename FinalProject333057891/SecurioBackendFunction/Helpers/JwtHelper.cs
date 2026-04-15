using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SecurioBackendFunction.Helpers
{
    // A utility for creating and validating cryptographically signed JSON Web Tokens used to authorize subsequent requests to protected data.
    public static class JwtHelper
    {
        private static readonly string Secret = GetSecret();

        // Reads the JwtSecret environment variable and throws if it is not configured.
        private static string GetSecret()
        {
            var secret = Environment.GetEnvironmentVariable("JwtSecret");
            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("JwtSecret environment variable is not configured.");
            return secret;
        }

        // Creates a signed JWT string for a specific user.
        public static string GenerateJwtToken(int userId, string username)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("user_id", userId.ToString()),
                new Claim("username", username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Validates a JWT token and returns the ClaimsPrincipal if valid, or null if invalid or expired.
        public static ClaimsPrincipal ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(Secret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }

        // Extracts the user_id claim from a validated ClaimsPrincipal. Returns 0 if extraction fails.
        public static int GetUserIdFromPrincipal(ClaimsPrincipal principal)
        {
            var claim = principal?.FindFirst("user_id");
            return int.TryParse(claim?.Value, out int userId) ? userId : 0;
        }
    }
}
