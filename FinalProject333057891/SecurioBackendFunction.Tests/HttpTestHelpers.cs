using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Newtonsoft.Json;
using SecurioBackendFunction.Helpers;

namespace SecurioBackendFunction.Tests
{
    // Shared test infrastructure used by HTTP function integration tests.
    // Provides helpers to build mock HttpRequests and generate valid JWTs.
    internal static class HttpTestHelpers
    {
        /// <summary>Builds a mock HttpRequest with an optional Authorization header and JSON body.</summary>
        internal static HttpRequest BuildRequest(string? authHeader, object? body)
        {
            var mock = new Mock<HttpRequest>();

            var headers = new HeaderDictionary();
            if (authHeader != null)
                headers.Add("Authorization", new StringValues(authHeader));
            mock.Setup(r => r.Headers).Returns(headers);

            var json   = body != null ? JsonConvert.SerializeObject(body) : string.Empty;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            mock.Setup(r => r.Body).Returns(stream);

            return mock.Object;
        }

        /// <summary>Generates a valid JWT for the given user ID (uses the test secret).</summary>
        internal static string Token(int userId = 1, string username = "testuser")
            => JwtHelper.GenerateJwtToken(userId, username);

        /// <summary>Shorthand to produce a Bearer token header value.</summary>
        internal static string Bearer(int userId = 1, string username = "testuser")
            => "Bearer " + Token(userId, username);
    }
}
