using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using SecurioBackendFunction.Helpers;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for the concrete HibpService class.
    // All HTTP calls are intercepted by FakeHttpMessageHandler — no real network access.
    // Tests cover: input validation, k-anonymity URL construction, response parsing,
    // case-insensitivity, and fail-open behaviour on network errors.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class HibpServiceTests
    {
        // SHA-1 of the empty string — a well-known leaked hash used as our "pwned" test value.
        // Prefix sent to HIBP: DA39A   Suffix matched in response: 3EE5E6B4B0D3255BFEF95601890AFD80709
        private const string PwnedHash   = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";
        private const string PwnedPrefix = "DA39A";
        private const string PwnedSuffix = "3EE5E6B4B0D3255BFEF95601890AFD80709";

        // A valid 40-char hex string whose suffix will NOT appear in mock responses.
        private const string SafeHash    = "AABBCCDDEEFF00112233445566778899AABBCCDD";

        // Builds a HibpService backed by a handler that returns the given body/status code.
        // Also captures every URL the service tried to request.
        private static (HibpService service, List<string> requestedUrls) Build(
            string responseBody, HttpStatusCode status = HttpStatusCode.OK)
        {
            var urls = new List<string>();
            var handler = new FakeHttpMessageHandler(req =>
            {
                urls.Add(req.RequestUri!.ToString());
                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8)
                };
            });
            return (new HibpService(new HttpClient(handler)), urls);
        }

        // ── Input validation ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task IsPasswordPwned_NullOrWhitespaceHash_ReturnsFalse(string? hash)
        {
            var (service, _) = Build("");
            Assert.False(await service.IsPasswordPwnedAsync(hash!));
        }

        [Theory]
        [InlineData("DA39A3EE5E6B4B0D3255BFEF95601890AFD8070")]    // 39 chars
        [InlineData("DA39A3EE5E6B4B0D3255BFEF95601890AFD807099")]  // 41 chars
        [InlineData("DA39A")]                                        // 5 chars
        public async Task IsPasswordPwned_WrongLengthHash_ReturnsFalse(string hash)
        {
            var (service, _) = Build("");
            Assert.False(await service.IsPasswordPwnedAsync(hash));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("DA39A3EE5E6B4B0D3255BFEF95601890AFD8070")]
        public async Task IsPasswordPwned_InvalidHash_NeverMakesHttpRequest(string? hash)
        {
            var (service, urls) = Build("");
            await service.IsPasswordPwnedAsync(hash!);
            Assert.Empty(urls);
        }

        // ── k-anonymity: only the 5-char prefix is sent over the network ─────────

        [Fact]
        public async Task IsPasswordPwned_UrlContainsOnlyPrefix_NotFullHash()
        {
            var (service, urls) = Build("");
            await service.IsPasswordPwnedAsync(PwnedHash);

            Assert.Single(urls);
            Assert.EndsWith(PwnedPrefix, urls[0]);
            Assert.DoesNotContain(PwnedSuffix, urls[0]);
        }

        [Fact]
        public async Task IsPasswordPwned_PrefixInUrlIsUppercase()
        {
            var (service, urls) = Build("");
            // Provide the hash in lowercase; the prefix sent must still be uppercase.
            await service.IsPasswordPwnedAsync(PwnedHash.ToLowerInvariant());

            Assert.Single(urls);
            Assert.Contains(PwnedPrefix, urls[0]);
        }

        // ── Response parsing: found ──────────────────────────────────────────────

        [Fact]
        public async Task IsPasswordPwned_SuffixPresentInResponse_ReturnsTrue()
        {
            string body = $"AAAAAA:10\r\n{PwnedSuffix}:98765\r\nBBBBBB:3";
            var (service, _) = Build(body);

            Assert.True(await service.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_SuffixInResponseLowercase_ReturnsTrue()
        {
            // Comparison must be case-insensitive regardless of the API response casing.
            string body = PwnedSuffix.ToLowerInvariant() + ":1234";
            var (service, _) = Build(body);

            Assert.True(await service.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_HashProvidedLowercase_ReturnsTrue()
        {
            string body = $"{PwnedSuffix}:1";
            var (service, _) = Build(body);

            // Client may send hash in any case — service must normalise before matching.
            Assert.True(await service.IsPasswordPwnedAsync(PwnedHash.ToLowerInvariant()));
        }

        // ── Response parsing: CRLF vs LF ────────────────────────────────────────

        [Fact]
        public async Task IsPasswordPwned_CrlfLineEndings_ParsedCorrectly()
        {
            string body = $"AAAAAA:1\r\n{PwnedSuffix}:999\r\nBBBBBB:2";
            var (service, _) = Build(body);

            Assert.True(await service.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_LfOnlyLineEndings_ParsedCorrectly()
        {
            string body = $"AAAAAA:1\n{PwnedSuffix}:999\nBBBBBB:2";
            var (service, _) = Build(body);

            Assert.True(await service.IsPasswordPwnedAsync(PwnedHash));
        }

        // ── Response parsing: not found ──────────────────────────────────────────

        [Fact]
        public async Task IsPasswordPwned_SuffixAbsentFromResponse_ReturnsFalse()
        {
            string body = "AAAAAA:5\r\nBBBBBB:3\r\nCCCCCC:1";
            var (service, _) = Build(body);

            Assert.False(await service.IsPasswordPwnedAsync(SafeHash));
        }

        [Fact]
        public async Task IsPasswordPwned_EmptyApiResponse_ReturnsFalse()
        {
            var (service, _) = Build("");
            Assert.False(await service.IsPasswordPwnedAsync(PwnedHash));
        }

        // ── Fail-open: network/server errors must never block a user ─────────────

        [Fact]
        public async Task IsPasswordPwned_HttpRequestThrows_ReturnsFalse()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                throw new HttpRequestException("simulated network failure"));
            var service = new HibpService(new HttpClient(handler));

            // Must not propagate the exception — should fail open (allow registration).
            Assert.False(await service.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_ApiReturns500_ReturnsFalse()
        {
            // GetStringAsync throws on non-2xx; the catch block must handle it gracefully.
            var (service, _) = Build("Internal Server Error", HttpStatusCode.InternalServerError);

            Assert.False(await service.IsPasswordPwnedAsync(PwnedHash));
        }

        // ── Minimal fake HttpMessageHandler ─────────────────────────────────────

        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

            public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
                => _respond = respond;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_respond(request));
        }
    }
}
