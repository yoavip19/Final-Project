using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioBackendFunction.ServerFunctions;
using SecurioModels;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests
{
    // Integration-style unit tests for VaultItemFunctions HTTP endpoints:
    //   AddVaultItem, UpdateVaultItem, GetVaultItems, DeleteVaultItem.
    public class VaultItemFunctionsTests
    {
        static VaultItemFunctionsTests()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test-jwt-secret-32-bytes-minimum!!");
        }

        private static VaultItem ValidItem() => new VaultItem
        {
            AccountName     = "GitHub",
            AccountUsername = "devuser@example.com",
            IV              = "aabbccddeeff0011",
            Tag             = "112233445566778899aabbccddeeff00",
            CipherText      = "encryptedPasswordBase64==",
            Sha1Hash        = "da39a3ee5e6b4b0d3255bfef95601890afd80709"
        };

        private static VaultItemFunctions Build(Mock<IVaultItemRepository>? repo = null)
            => new VaultItemFunctions(new VaultItemManager((repo ?? new Mock<IVaultItemRepository>()).Object));

        // ── AddVaultItem: auth checks ────────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_NoAuthHeader_Returns401()
        {
            var result = await Build().AddVaultItem(HttpTestHelpers.BuildRequest(null, ValidItem()));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_NonBearerScheme_Returns401()
        {
            var result = await Build().AddVaultItem(HttpTestHelpers.BuildRequest("Basic dXNlcjpwYXNz", ValidItem()));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_TamperedToken_Returns401()
        {
            var result = await Build().AddVaultItem(HttpTestHelpers.BuildRequest("Bearer this.is.invalid", ValidItem()));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_UnauthorizedResult_HasErrorMessage()
        {
            var result = await Build().AddVaultItem(HttpTestHelpers.BuildRequest(null, ValidItem()));
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(unauthorized.Value);
            Assert.False(response.Success);
            Assert.Equal("Unauthorized.", response.Message);
        }

        // ── AddVaultItem: empty body ─────────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_EmptyBody_Returns400()
        {
            var result = await Build().AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), null));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── AddVaultItem: happy path ─────────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_ValidRequest_Returns200()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(10);

            var result = await Build(repo).AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), ValidItem()));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(ok.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task AddVaultItem_ValidRequest_ReturnedDataHasAssignedId()
        {
            const int assignedId = 42;
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(assignedId);

            var result = await Build(repo).AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), ValidItem()));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(ok.Value);
            Assert.Equal(assignedId, response.Data!.Id);
        }

        // ── AddVaultItem: security — UserId binding ──────────────────────────────

        [Fact]
        public async Task AddVaultItem_ClientSuppliedUserIdIgnored_TokenUserIdBinds()
        {
            const int tokenUserId = 7;
            VaultItem? captured = null;

            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>()))
                .Callback<VaultItem>(i => captured = i)
                .ReturnsAsync(1);

            var item = ValidItem();
            item.UserId = 99; // attacker-supplied value
            await Build(repo).AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(tokenUserId), item));

            Assert.NotNull(captured);
            Assert.Equal(tokenUserId, captured!.UserId);
        }

        // ── AddVaultItem: validation errors ──────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_MissingAccountName_Returns400WithMessage()
        {
            var item = ValidItem();
            item.AccountName = null!;
            var result = await Build().AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), item));
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(bad.Value);
            Assert.Equal("Account name is required.", response.Message);
        }

        [Fact]
        public async Task AddVaultItem_MissingCipherText_Returns400()
        {
            var item = ValidItem();
            item.CipherText = null!;
            var result = await Build().AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), item));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_MissingIV_Returns400()
        {
            var item = ValidItem();
            item.IV = null!;
            var result = await Build().AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), item));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_MissingTag_Returns400()
        {
            var item = ValidItem();
            item.Tag = null!;
            var result = await Build().AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), item));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddVaultItem_MissingSha1Hash_Returns400()
        {
            var item = ValidItem();
            item.Sha1Hash = null!;
            var result = await Build().AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), item));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── AddVaultItem: database errors ─────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_RepositoryReturnsZero_Returns400WithDatabaseError()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(0);

            var result = await Build(repo).AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), ValidItem()));
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(bad.Value);
            Assert.Equal("Database error.", response.Message);
        }

        [Fact]
        public async Task AddVaultItem_RepositoryThrows_Returns400WithInternalError()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>()))
                .ThrowsAsync(new InvalidOperationException("connection lost"));

            var result = await Build(repo).AddVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), ValidItem()));
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<VaultItem>>(bad.Value);
            Assert.Equal("An internal error occurred.", response.Message);
        }

        // ── UpdateVaultItem ───────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateVaultItem_NoAuthHeader_Returns401()
        {
            var item = ValidItem();
            item.Id = 1;
            var result = await Build().UpdateVaultItem(HttpTestHelpers.BuildRequest(null, item));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateVaultItem_ValidRequest_Returns200()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id = 5;

            var result = await Build(repo).UpdateVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), item));
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateVaultItem_ItemNotFound_Returns400()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(false);

            var item = ValidItem();
            item.Id = 9999;

            var result = await Build(repo).UpdateVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), item));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateVaultItem_SecurityUserIdFromToken()
        {
            const int tokenUserId = 7;
            VaultItem? captured = null;

            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>()))
                .Callback<VaultItem>(i => captured = i)
                .ReturnsAsync(true);

            var item = ValidItem();
            item.Id = 1;
            item.UserId = 99; // attacker-supplied
            await Build(repo).UpdateVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(tokenUserId), item));

            Assert.NotNull(captured);
            Assert.Equal(tokenUserId, captured!.UserId);
        }

        // ── GetVaultItems ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetVaultItems_NoAuthHeader_Returns401()
        {
            var result = await Build().GetVaultItems(HttpTestHelpers.BuildRequest(null, null));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetVaultItems_ValidToken_Returns200WithItems()
        {
            var items = new List<VaultItem> { ValidItem(), ValidItem() };
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.GetVaultItemsByUserIdAsync(It.IsAny<int>())).ReturnsAsync(items);

            var result = await Build(repo).GetVaultItems(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), null));

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<List<VaultItem>>>(ok.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
        }

        [Fact]
        public async Task GetVaultItems_EmptyVault_Returns200WithEmptyList()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.GetVaultItemsByUserIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<VaultItem>());

            var result = await Build(repo).GetVaultItems(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), null));

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<List<VaultItem>>>(ok.Value);
            Assert.Empty(response.Data!);
        }

        [Fact]
        public async Task GetVaultItems_FetchesItemsForTokenUser()
        {
            const int tokenUserId = 7;
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.GetVaultItemsByUserIdAsync(tokenUserId)).ReturnsAsync(new List<VaultItem>());

            await Build(repo).GetVaultItems(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(tokenUserId), null));

            repo.Verify(r => r.GetVaultItemsByUserIdAsync(tokenUserId), Times.Once);
        }

        // ── DeleteVaultItem ───────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteVaultItem_NoAuthHeader_Returns401()
        {
            var result = await Build().DeleteVaultItem(
                HttpTestHelpers.BuildRequest(null, new { Id = 1 }));
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task DeleteVaultItem_ValidRequest_Returns200()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.DeleteVaultItemAsync(42, It.IsAny<int>())).ReturnsAsync(true);

            var result = await Build(repo).DeleteVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), new { Id = 42 }));
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteVaultItem_ItemNotFound_Returns400()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.DeleteVaultItemAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            var result = await Build(repo).DeleteVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), new { Id = 9999 }));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteVaultItem_ZeroId_Returns400WithItemIdRequired()
        {
            var result = await Build().DeleteVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), new { Id = 0 }));
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<object>>(bad.Value);
            Assert.Equal("Item ID is required.", response.Message);
        }

        [Fact]
        public async Task DeleteVaultItem_EmptyBody_Returns400()
        {
            var result = await Build().DeleteVaultItem(
                HttpTestHelpers.BuildRequest(HttpTestHelpers.Bearer(), null));
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
