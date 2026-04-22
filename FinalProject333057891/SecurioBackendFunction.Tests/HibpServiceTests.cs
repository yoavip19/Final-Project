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
    // Unit tests for HibpService — the server-side HIBP k-anonymity checker.
    // All HTTP calls are intercepted; no real network access occurs.
    public class HibpServiceTests
    {
        // SHA-1 of the empty string — a well-known hash appearing in the HIBP dataset.
        private const string PwnedHash   = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";
        private const string PwnedPrefix = "DA39A";
        private const string PwnedSuffix = "3EE5E6B4B0D3255BFEF95601890AFD80709";

        // A valid 40-char hex string whose suffix is absent from mock responses.
        private const string SafeHash    = "AABBCCDDEEFF00112233445566778899AABBCCDD";

        private static (HibpService Service, List<string> RequestedUrls) Build(
            string responseBody, HttpStatusCode status = HttpStatusCode.OK)
        {
            var urls = new List<string>();
            var handler = new FakeHandler(req =>
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
        public async Task IsPasswordPwned_NullOrWhitespace_ReturnsFalse(string? hash)
        {
            var (svc, _) = Build("");
            Assert.False(await svc.IsPasswordPwnedAsync(hash!));
        }

        [Theory]
        [InlineData("DA39A3EE5E6B4B0D3255BFEF95601890AFD8070")]   // 39 chars
        [InlineData("DA39A3EE5E6B4B0D3255BFEF95601890AFD807099")] // 41 chars
        [InlineData("DA39A")]                                       // 5 chars
        [InlineData("GGGG03EE5E6B4B0D3255BFEF95601890AFD80709")]   // non-hex chars
        public async Task IsPasswordPwned_WrongLength_ReturnsFalse(string hash)
        {
            var (svc, _) = Build("");
            Assert.False(await svc.IsPasswordPwnedAsync(hash));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("DA39A3EE5E6B4B0D3255BFEF95601890AFD8070")]
        public async Task IsPasswordPwned_InvalidHash_NeverMakesHttpRequest(string? hash)
        {
            var (svc, urls) = Build("");
            await svc.IsPasswordPwnedAsync(hash!);
            Assert.Empty(urls);
        }

        // ── k-anonymity: only the 5-char prefix is sent ─────────────────────────

        [Fact]
        public async Task IsPasswordPwned_UrlContainsOnlyPrefix_NotFullHash()
        {
            var (svc, urls) = Build("");
            await svc.IsPasswordPwnedAsync(PwnedHash);
            Assert.Single(urls);
            Assert.EndsWith(PwnedPrefix, urls[0]);
            Assert.DoesNotContain(PwnedSuffix, urls[0]);
        }

        [Fact]
        public async Task IsPasswordPwned_PrefixInUrlIsUppercase()
        {
            var (svc, urls) = Build("");
            await svc.IsPasswordPwnedAsync(PwnedHash.ToLowerInvariant());
            Assert.Single(urls);
            Assert.Contains(PwnedPrefix, urls[0]);
        }

        [Fact]
        public async Task IsPasswordPwned_MakesExactlyOneHttpRequest()
        {
            var (svc, urls) = Build($"{PwnedSuffix}:1");
            await svc.IsPasswordPwnedAsync(PwnedHash);
            Assert.Single(urls);
        }

        // ── Response parsing: hash found ─────────────────────────────────────────

        [Fact]
        public async Task IsPasswordPwned_SuffixPresentInResponse_ReturnsTrue()
        {
            string body = $"AAAAAA:10\r\n{PwnedSuffix}:98765\r\nBBBBBB:3";
            var (svc, _) = Build(body);
            Assert.True(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_SuffixInResponseLowercase_ReturnsTrue()
        {
            string body = PwnedSuffix.ToLowerInvariant() + ":1234";
            var (svc, _) = Build(body);
            Assert.True(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_HashProvidedLowercase_ReturnsTrue()
        {
            string body = $"{PwnedSuffix}:1";
            var (svc, _) = Build(body);
            Assert.True(await svc.IsPasswordPwnedAsync(PwnedHash.ToLowerInvariant()));
        }

        [Fact]
        public async Task IsPasswordPwned_SingleLineResponse_ParsedCorrectly()
        {
            string body = $"{PwnedSuffix}:1";
            var (svc, _) = Build(body);
            Assert.True(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        // ── Response parsing: line endings ───────────────────────────────────────

        [Fact]
        public async Task IsPasswordPwned_CrlfLineEndings_ParsedCorrectly()
        {
            string body = $"AAAAAA:1\r\n{PwnedSuffix}:999\r\nBBBBBB:2";
            var (svc, _) = Build(body);
            Assert.True(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_LfOnlyLineEndings_ParsedCorrectly()
        {
            string body = $"AAAAAA:1\n{PwnedSuffix}:999\nBBBBBB:2";
            var (svc, _) = Build(body);
            Assert.True(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        // ── Response parsing: hash not found ─────────────────────────────────────

        [Fact]
        public async Task IsPasswordPwned_SuffixAbsent_ReturnsFalse()
        {
            string body = "AAAAAA:5\r\nBBBBBB:3\r\nCCCCCC:1";
            var (svc, _) = Build(body);
            Assert.False(await svc.IsPasswordPwnedAsync(SafeHash));
        }

        [Fact]
        public async Task IsPasswordPwned_EmptyApiResponse_ReturnsFalse()
        {
            var (svc, _) = Build("");
            Assert.False(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_ResponseHasLargePwnCount_StillReturnsTrue()
        {
            string body = $"{PwnedSuffix}:9999999";
            var (svc, _) = Build(body);
            Assert.True(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_ResponseHasZeroCount_StillReturnsTrue()
        {
            // Even count of 0 means the suffix matched — counts are not filtered.
            string body = $"{PwnedSuffix}:0";
            var (svc, _) = Build(body);
            Assert.True(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        // ── Fail-open behaviour ──────────────────────────────────────────────────

        [Fact]
        public async Task IsPasswordPwned_HttpRequestThrows_ReturnsFalse()
        {
            var handler = new FakeHandler(_ => throw new HttpRequestException("network failure"));
            var svc = new HibpService(new HttpClient(handler));
            Assert.False(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_ApiReturns500_ReturnsFalse()
        {
            var (svc, _) = Build("Internal Server Error", HttpStatusCode.InternalServerError);
            Assert.False(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        [Fact]
        public async Task IsPasswordPwned_ApiReturns404_ReturnsFalse()
        {
            var (svc, _) = Build("Not Found", HttpStatusCode.NotFound);
            Assert.False(await svc.IsPasswordPwnedAsync(PwnedHash));
        }

        // ── Minimal fake HttpMessageHandler ─────────────────────────────────────

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
            public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
                => _respond = respond;
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_respond(request));
        }
    }
}
