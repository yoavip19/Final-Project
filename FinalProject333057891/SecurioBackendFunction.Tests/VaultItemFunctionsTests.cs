using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Newtonsoft.Json;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioBackendFunction.ServerFunctions;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using Xunit;

namespace SecurioBackendFunction.Tests
{
    // Integration-style unit tests for the AddVaultItem HTTP endpoint.
    // All I/O (database, JWT secrets) is replaced by test doubles so no
    // real infrastructure is needed.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class VaultItemFunctionsTests
    {
        // Must be set before JwtHelper's static field is first read.
        static VaultItemFunctionsTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        // Returns a fully-populated valid vault item (mirrors AddPasswordActivity output).
        private static VaultItem ValidItem() => new VaultItem
        {
            AccountName     = "GitHub",
            AccountUsername = "devuser@example.com",
            IV              = "aabbccddeeff0011",
            Tag             = "112233445566778899aabbccddeeff00",
            CipherText      = "encryptedPasswordBase64==",
            Sha1Hash        = "da39a3ee5e6b4b0d3255bfef95601890afd80709",
        };

        // Builds a mock HttpRequest with the given Authorization header and a JSON-serialised body.
        private static HttpRequest BuildRequest(string? authHeader, object? body)
        {
            var mock = new Mock<HttpRequest>();

            var headers = new HeaderDictionary();
            if (authHeader != null)
                headers.Add("Authorization", new StringValues(authHeader));
            mock.Setup(r => r.Headers).Returns(headers);

            var json    = body != null ? JsonConvert.SerializeObject(body) : string.Empty;
            var stream  = new MemoryStream(Encoding.UTF8.GetBytes(json));
            mock.Setup(r => r.Body).Returns(stream);

            return mock.Object;
        }

        // Builds the function under test wired to the supplied (or default no-op) repository mock.
        private static VaultItemFunctions BuildFunctions(Mock<IVaultItemRepository>? repoMock = null)
        {
            repoMock ??= new Mock<IVaultItemRepository>();
            return new VaultItemFunctions(new VaultItemManager(repoMock.Object));
        }

        // Returns a valid JWT for the given user.
        private static string Token(int userId = 1, string username = "testuser")
            => JwtHelper.GenerateJwtToken(userId, username);

        // ── Authorization checks ─────────────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_NoAuthorizationHeader_Returns401()
        {
            var req = BuildRequest(null, ValidItem());

            var result = await BuildFunctions().AddVaultItem(req);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_NonBearerScheme_Returns401()
        {
            // "Basic" scheme must be rejected — only Bearer tokens are accepted.
            var req = BuildRequest("Basic dXNlcjpwYXNz", ValidItem());

            var result = await BuildFunctions().AddVaultItem(req);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_TamperedToken_Returns401()
        {
            var req = BuildRequest("Bearer this.is.not.a.valid.jwt", ValidItem());

            var result = await BuildFunctions().AddVaultItem(req);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_UnauthorizedResult_ContainsErrorMessage()
        {
            var req = BuildRequest(null, ValidItem());

            var result      = await BuildFunctions().AddVaultItem(req);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var response    = Assert.IsType<ServerResponse<VaultItem>>(unauthorized.Value);

            Assert.False(response.Success);
            Assert.Equal("Unauthorized.", response.Message);
        }

        // ── Request body checks ──────────────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_EmptyBody_Returns400()
        {
            // An empty body deserializes to null — the function must reject it.
            var req = BuildRequest("Bearer " + Token(), null);

            var result = await BuildFunctions().AddVaultItem(req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── Happy path ────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_ValidRequest_Returns200WithSuccess()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(10);

            var req    = BuildRequest("Bearer " + Token(), ValidItem());
            var result = await BuildFunctions(repoMock).AddVaultItem(req);

            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(ok.Value);
            Assert.True(response.Success);
            Assert.Equal("Vault item added successfully.", response.Message);
        }

        [Fact]
        public async Task AddVaultItem_ValidRequest_ReturnedDataContainsAssignedId()
        {
            const int assignedId = 42;
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(assignedId);

            var req    = BuildRequest("Bearer " + Token(), ValidItem());
            var result = await BuildFunctions(repoMock).AddVaultItem(req);

            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(ok.Value);
            Assert.Equal(assignedId, response.Data.Id);
        }

        // ── Security: UserId binding ──────────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_ClientSuppliedUserIdIgnored_TokenUserIdUsed()
        {
            // The client sends UserId=99 in the body, but the authenticated token
            // belongs to user 7.  The server must overwrite the body value to
            // prevent privilege escalation.
            const int tokenUserId = 7;
            VaultItem? capturedItem = null;

            var repoMock = new Mock<IVaultItemRepository>();
            repoMock
                .Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>()))
                .Callback<VaultItem>(i => capturedItem = i)
                .ReturnsAsync(1);

            var item = ValidItem();
            item.UserId = 99; // attacker-controlled value
            var req = BuildRequest("Bearer " + Token(tokenUserId), item);
            await BuildFunctions(repoMock).AddVaultItem(req);

            Assert.NotNull(capturedItem);
            Assert.Equal(tokenUserId, capturedItem!.UserId);
        }

        // ── Validation errors propagated from VaultItemManager ───────────────────

        [Fact]
        public async Task AddVaultItem_MissingAccountName_Returns400WithMessage()
        {
            var item = ValidItem();
            item.AccountName = null!;

            var req    = BuildRequest("Bearer " + Token(), item);
            var result = await BuildFunctions().AddVaultItem(req);

            var bad      = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(bad.Value);
            Assert.False(response.Success);
            Assert.Equal("Account name is required.", response.Message);
        }

        [Fact]
        public async Task AddVaultItem_MissingCipherText_Returns400()
        {
            var item = ValidItem();
            item.CipherText = null!;

            var req    = BuildRequest("Bearer " + Token(), item);
            var result = await BuildFunctions().AddVaultItem(req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_MissingIV_Returns400()
        {
            var item = ValidItem();
            item.IV = null!;

            var req    = BuildRequest("Bearer " + Token(), item);
            var result = await BuildFunctions().AddVaultItem(req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_MissingTag_Returns400()
        {
            var item = ValidItem();
            item.Tag = null!;

            var req    = BuildRequest("Bearer " + Token(), item);
            var result = await BuildFunctions().AddVaultItem(req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_MissingSha1Hash_Returns400()
        {
            var item = ValidItem();
            item.Sha1Hash = null!;

            var req    = BuildRequest("Bearer " + Token(), item);
            var result = await BuildFunctions().AddVaultItem(req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── Database / repository errors ─────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_RepositoryReturnsZero_Returns400WithDatabaseError()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(0);

            var req    = BuildRequest("Bearer " + Token(), ValidItem());
            var result = await BuildFunctions(repoMock).AddVaultItem(req);

            var bad      = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(bad.Value);
            Assert.False(response.Success);
            Assert.Equal("Database error.", response.Message);
        }

        [Fact]
        public async Task AddVaultItem_RepositoryThrows_Returns400WithInternalError()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock
                .Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>()))
                .ThrowsAsync(new InvalidOperationException("connection lost"));

            var req    = BuildRequest("Bearer " + Token(), ValidItem());
            var result = await BuildFunctions(repoMock).AddVaultItem(req);

            var bad      = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(bad.Value);
            Assert.False(response.Success);
            Assert.Equal("An internal error occurred.", response.Message);
        }
    }
}
