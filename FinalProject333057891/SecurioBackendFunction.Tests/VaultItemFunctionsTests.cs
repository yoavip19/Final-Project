using System.IO;
using System.Text;
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
    // Integration-style tests for the VaultItemFunctions HTTP endpoints.
    // Exercises AddVaultItem, UpdateVaultItem, GetVaultItems, and DeleteVaultItem
    // through the full HTTP → Manager → (mocked) Repository chain.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class VaultItemFunctionsTests
    {
        static VaultItemFunctionsTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static VaultItem ValidItem() => new VaultItem
        {
            AccountName     = "GitHub",
            AccountUsername = "devuser@example.com",
            IV              = "aabbccddeeff0011",
            Tag             = "112233445566778899aabbccddeeff00",
            CipherText      = "encryptedPasswordBase64==",
            Sha1Hash        = "da39a3ee5e6b4b0d3255bfef95601890afd80709"
        };

        private static HttpRequest BuildRequest(string? authHeader, object? body)
        {
            var mock    = new Mock<HttpRequest>();
            var headers = new HeaderDictionary();
            if (authHeader != null)
                headers.Add("Authorization", new StringValues(authHeader));
            mock.Setup(r => r.Headers).Returns(headers);

            var json   = body != null ? JsonConvert.SerializeObject(body) : string.Empty;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            mock.Setup(r => r.Body).Returns(stream);
            return mock.Object;
        }

        private static VaultItemFunctions BuildFunctions(Mock<IVaultItemRepository>? repo = null)
        {
            repo ??= new Mock<IVaultItemRepository>();
            return new VaultItemFunctions(new VaultItemManager(repo.Object));
        }

        private static string Token(int userId = 1, string username = "testuser")
            => JwtHelper.GenerateJwtToken(userId, username);

        // ── AddVaultItem: authorization ───────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_NoAuthHeader_Returns401()
        {
            var result = await BuildFunctions().AddVaultItem(BuildRequest(null, ValidItem()));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_NonBearerScheme_Returns401()
        {
            var result = await BuildFunctions().AddVaultItem(BuildRequest("Basic dXNlcjpwYXNz", ValidItem()));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_TamperedToken_Returns401()
        {
            var result = await BuildFunctions().AddVaultItem(BuildRequest("Bearer this.is.not.valid", ValidItem()));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_Unauthorized_ResponseContainsMessage()
        {
            var result       = await BuildFunctions().AddVaultItem(BuildRequest(null, ValidItem()));
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var response     = Assert.IsType<ServerResponse<VaultItem>>(unauthorized.Value);

            Assert.False(response.Success);
            Assert.Equal("Unauthorized.", response.Message);
        }

        // ── AddVaultItem: body validation ─────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_EmptyBody_Returns400()
        {
            var result = await BuildFunctions().AddVaultItem(BuildRequest("Bearer " + Token(), null));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── AddVaultItem: happy path ──────────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_ValidRequest_Returns200WithSuccess()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(10);

            var result   = await BuildFunctions(repo).AddVaultItem(BuildRequest("Bearer " + Token(), ValidItem()));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Vault item added successfully.", response.Message);
        }

        [Fact]
        public async Task AddVaultItem_ValidRequest_ReturnedDataContainsAssignedId()
        {
            const int assignedId = 42;
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(assignedId);

            var result   = await BuildFunctions(repo).AddVaultItem(BuildRequest("Bearer " + Token(), ValidItem()));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(ok.Value);

            Assert.Equal(assignedId, response.Data!.Id);
        }

        // ── AddVaultItem: UserId binding security ─────────────────────────────────

        [Fact]
        public async Task AddVaultItem_ClientSuppliedUserIdIgnored_TokenUserIdUsed()
        {
            const int tokenUserId = 7;
            VaultItem? captured = null;

            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>()))
                .Callback<VaultItem>(i => captured = i)
                .ReturnsAsync(1);

            var item = ValidItem();
            item.UserId = 99; // attacker-supplied value
            await BuildFunctions(repo).AddVaultItem(BuildRequest("Bearer " + Token(tokenUserId), item));

            Assert.NotNull(captured);
            Assert.Equal(tokenUserId, captured!.UserId);
        }

        // ── AddVaultItem: validation error propagation ────────────────────────────

        [Fact]
        public async Task AddVaultItem_MissingAccountName_Returns400WithMessage()
        {
            var item = ValidItem();
            item.AccountName = null!;

            var result   = await BuildFunctions().AddVaultItem(BuildRequest("Bearer " + Token(), item));
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

            var result = await BuildFunctions().AddVaultItem(BuildRequest("Bearer " + Token(), item));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_MissingIV_Returns400()
        {
            var item = ValidItem();
            item.IV = null!;

            var result = await BuildFunctions().AddVaultItem(BuildRequest("Bearer " + Token(), item));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_MissingTag_Returns400()
        {
            var item = ValidItem();
            item.Tag = null!;

            var result = await BuildFunctions().AddVaultItem(BuildRequest("Bearer " + Token(), item));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_MissingSha1Hash_Returns400()
        {
            var item = ValidItem();
            item.Sha1Hash = null!;

            var result = await BuildFunctions().AddVaultItem(BuildRequest("Bearer " + Token(), item));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── AddVaultItem: database / repository errors ────────────────────────────

        [Fact]
        public async Task AddVaultItem_DatabaseReturnsZero_Returns400WithDatabaseError()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(0);

            var result   = await BuildFunctions(repo).AddVaultItem(BuildRequest("Bearer " + Token(), ValidItem()));
            var bad      = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(bad.Value);

            Assert.False(response.Success);
            Assert.Equal("Database error.", response.Message);
        }

        [Fact]
        public async Task AddVaultItem_RepositoryThrows_Returns400WithInternalError()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>()))
                .ThrowsAsync(new InvalidOperationException("connection lost"));

            var result   = await BuildFunctions(repo).AddVaultItem(BuildRequest("Bearer " + Token(), ValidItem()));
            var bad      = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(bad.Value);

            Assert.False(response.Success);
            Assert.Equal("An internal error occurred.", response.Message);
        }

        // ── UpdateVaultItem: authorization ────────────────────────────────────────

        [Fact]
        public async Task UpdateVaultItem_NoAuthHeader_Returns401()
        {
            var item = ValidItem();
            item.Id = 1;
            var result = await BuildFunctions().UpdateVaultItem(BuildRequest(null, item));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateVaultItem_TamperedToken_Returns401()
        {
            var item = ValidItem();
            item.Id = 1;
            var result = await BuildFunctions().UpdateVaultItem(BuildRequest("Bearer bad.token", item));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── UpdateVaultItem: happy path ───────────────────────────────────────────

        [Fact]
        public async Task UpdateVaultItem_ValidRequest_Returns200WithSuccess()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id = 5;
            var result   = await BuildFunctions(repo).UpdateVaultItem(BuildRequest("Bearer " + Token(), item));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(ok.Value);

            Assert.True(response.Success);
        }

        // ── UpdateVaultItem: error paths ──────────────────────────────────────────

        [Fact]
        public async Task UpdateVaultItem_ItemNotFound_Returns400()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(false);

            var item = ValidItem();
            item.Id = 999;
            var result = await BuildFunctions(repo).UpdateVaultItem(BuildRequest("Bearer " + Token(), item));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateVaultItem_EmptyBody_Returns400()
        {
            var result = await BuildFunctions().UpdateVaultItem(BuildRequest("Bearer " + Token(), null));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── GetVaultItems: authorization ──────────────────────────────────────────

        [Fact]
        public async Task GetVaultItems_NoAuthHeader_Returns401()
        {
            var result = await BuildFunctions().GetVaultItems(BuildRequest(null, null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetVaultItems_TamperedToken_Returns401()
        {
            var result = await BuildFunctions().GetVaultItems(BuildRequest("Bearer bad.token", null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── GetVaultItems: happy path ─────────────────────────────────────────────

        [Fact]
        public async Task GetVaultItems_ValidToken_Returns200WithItems()
        {
            var items = new List<VaultItem> { ValidItem(), ValidItem() };
            var repo  = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.GetVaultItemsByUserIdAsync(It.IsAny<int>())).ReturnsAsync(items);

            var result   = await BuildFunctions(repo).GetVaultItems(BuildRequest("Bearer " + Token(), null));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<List<VaultItem>>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
        }

        [Fact]
        public async Task GetVaultItems_EmptyVault_Returns200WithEmptyList()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.GetVaultItemsByUserIdAsync(It.IsAny<int>())).ReturnsAsync(new List<VaultItem>());

            var result   = await BuildFunctions(repo).GetVaultItems(BuildRequest("Bearer " + Token(), null));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<List<VaultItem>>>(ok.Value);

            Assert.True(response.Success);
            Assert.Empty(response.Data!);
        }

        // ── DeleteVaultItem: authorization ────────────────────────────────────────

        [Fact]
        public async Task DeleteVaultItem_NoAuthHeader_Returns401()
        {
            var result = await BuildFunctions().DeleteVaultItem(BuildRequest(null, new { Id = 1 }));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── DeleteVaultItem: body validation ─────────────────────────────────────

        [Fact]
        public async Task DeleteVaultItem_EmptyBody_Returns400()
        {
            var result = await BuildFunctions().DeleteVaultItem(BuildRequest("Bearer " + Token(), null));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteVaultItem_ZeroItemId_Returns400()
        {
            var result = await BuildFunctions().DeleteVaultItem(BuildRequest("Bearer " + Token(), new { Id = 0 }));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── DeleteVaultItem: happy path ───────────────────────────────────────────

        [Fact]
        public async Task DeleteVaultItem_ValidRequest_Returns200WithSuccess()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.DeleteVaultItemAsync(5, It.IsAny<int>())).ReturnsAsync(true);

            var result   = await BuildFunctions(repo).DeleteVaultItem(BuildRequest("Bearer " + Token(), new { Id = 5 }));
            var ok       = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<object>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Vault item deleted successfully.", response.Message);
        }

        // ── DeleteVaultItem: not found ────────────────────────────────────────────

        [Fact]
        public async Task DeleteVaultItem_ItemNotFound_Returns400()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.DeleteVaultItemAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);

            var result = await BuildFunctions(repo).DeleteVaultItem(BuildRequest("Bearer " + Token(), new { Id = 999 }));
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
