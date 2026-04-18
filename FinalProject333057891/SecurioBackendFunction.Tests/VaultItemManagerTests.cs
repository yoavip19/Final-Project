using Moq;
using Xunit;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;
using System.Collections.Generic;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for VaultItemManager.
    // All tests use a mocked IVaultItemRepository — no real database is required.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class VaultItemManagerTests
    {
        // Returns a fully-populated valid VaultItem that satisfies every validation rule.
        private static VaultItem ValidItem() => new VaultItem
        {
            UserId          = 1,
            AccountName     = "Gmail",
            AccountUsername = "user@gmail.com",
            IV              = "aabbccddeeff0011",
            Tag             = "112233445566778899aabbccddeeff00",
            CipherText      = "encryptedPasswordBase64==",
            Sha1Hash        = "da39a3ee5e6b4b0d3255bfef95601890afd80709",
            IsLeaked        = false
        };

        // ── Happy path ──────────────────────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_ValidItem_ReturnsSuccess()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(42);
            var manager = new VaultItemManager(repoMock.Object);

            var result = await manager.AddVaultItemAsync(ValidItem());

            Assert.True(result.Success);
            Assert.Equal("Vault item added successfully.", result.Message);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task AddVaultItem_ValidItem_SetsIdOnReturnedData()
        {
            const int expectedId = 99;
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(expectedId);
            var manager = new VaultItemManager(repoMock.Object);

            var result = await manager.AddVaultItemAsync(ValidItem());

            Assert.Equal(expectedId, result.Data.Id);
        }

        [Fact]
        public async Task AddVaultItem_OptionalFieldsNull_StillSucceeds()
        {
            var item = ValidItem();
            item.AccountUsername = null;
            item.Notes           = null;

            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(5);
            var manager = new VaultItemManager(repoMock.Object);

            var result = await manager.AddVaultItemAsync(item);

            Assert.True(result.Success);
        }

        // ── Validation failures ─────────────────────────────────────────────────

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task AddVaultItem_MissingAccountName_ReturnsFail(string? accountName)
        {
            var item = ValidItem();
            item.AccountName = accountName!;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.AddVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Account name is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task AddVaultItem_MissingCipherText_ReturnsFail(string? cipherText)
        {
            var item = ValidItem();
            item.CipherText = cipherText!;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.AddVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("CipherText is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task AddVaultItem_MissingIV_ReturnsFail(string? iv)
        {
            var item = ValidItem();
            item.IV = iv!;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.AddVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("IV is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task AddVaultItem_MissingTag_ReturnsFail(string? tag)
        {
            var item = ValidItem();
            item.Tag = tag!;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.AddVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Tag is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task AddVaultItem_MissingHash_ReturnsFail(string? sha1Hash)
        {
            var item = ValidItem();
            item.Sha1Hash = sha1Hash!;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.AddVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Sha1Hash is required.", result.Message);
        }

        // ── Repository / database errors ────────────────────────────────────────

        [Fact]
        public async Task AddVaultItem_RepositoryReturnsZero_ReturnsDatabaseError()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(0);
            var manager = new VaultItemManager(repoMock.Object);

            var result = await manager.AddVaultItemAsync(ValidItem());

            Assert.False(result.Success);
            Assert.Equal("Database error.", result.Message);
        }

        [Fact]
        public async Task AddVaultItem_RepositoryReturnsNegative_ReturnsDatabaseError()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(-1);
            var manager = new VaultItemManager(repoMock.Object);

            var result = await manager.AddVaultItemAsync(ValidItem());

            Assert.False(result.Success);
            Assert.Equal("Database error.", result.Message);
        }

        // ── UpdateVaultItemAsync — happy path ───────────────────────────────────

        [Fact]
        public async Task UpdateVaultItem_ValidItem_ReturnsSuccess()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);
            var manager = new VaultItemManager(repoMock.Object);

            var item = ValidItem();
            item.Id = 42;
            var result = await manager.UpdateVaultItemAsync(item);

            Assert.True(result.Success);
            Assert.Equal("Vault item updated successfully.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(42, result.Data.Id);
        }

        // ── UpdateVaultItemAsync — validation failures ──────────────────────────

        [Fact]
        public async Task UpdateVaultItem_ZeroId_ReturnsFail()
        {
            var item = ValidItem();
            item.Id = 0;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Item ID is required.", result.Message);
        }

        [Fact]
        public async Task UpdateVaultItem_NegativeId_ReturnsFail()
        {
            var item = ValidItem();
            item.Id = -1;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Item ID is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateVaultItem_MissingAccountName_ReturnsFail(string? accountName)
        {
            var item = ValidItem();
            item.Id = 1;
            item.AccountName = accountName!;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Account name is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateVaultItem_MissingCipherText_ReturnsFail(string? cipherText)
        {
            var item = ValidItem();
            item.Id = 1;
            item.CipherText = cipherText!;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("CipherText is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateVaultItem_MissingIV_ReturnsFail(string? iv)
        {
            var item = ValidItem();
            item.Id = 1;
            item.IV = iv!;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("IV is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateVaultItem_MissingTag_ReturnsFail(string? tag)
        {
            var item = ValidItem();
            item.Id = 1;
            item.Tag = tag!;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Tag is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateVaultItem_MissingHash_ReturnsFail(string? sha1Hash)
        {
            var item = ValidItem();
            item.Id = 1;
            item.Sha1Hash = sha1Hash!;
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Sha1Hash is required.", result.Message);
        }

        // ── UpdateVaultItemAsync — repository failures ──────────────────────────

        [Fact]
        public async Task UpdateVaultItem_RepositoryReturnsFalse_ReturnsNotFound()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(false);
            var manager = new VaultItemManager(repoMock.Object);

            var item = ValidItem();
            item.Id = 999;
            var result = await manager.UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Item not found or access denied.", result.Message);
        }

        // ── GetVaultItemsAsync — happy path ─────────────────────────────────────

        [Fact]
        public async Task GetVaultItems_ValidUserId_ReturnsSuccess()
        {
            var items = new List<VaultItem> { ValidItem(), ValidItem() };
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);
            var manager = new VaultItemManager(repoMock.Object);

            var result = await manager.GetVaultItemsAsync(1);

            Assert.True(result.Success);
            Assert.Equal("Vault items retrieved successfully.", result.Message);
            Assert.Equal(2, result.Data.Count);
        }

        [Fact]
        public async Task GetVaultItems_NoItems_ReturnsEmptyList()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());
            var manager = new VaultItemManager(repoMock.Object);

            var result = await manager.GetVaultItemsAsync(1);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        // ── GetVaultItemsAsync — validation failures ────────────────────────────

        [Fact]
        public async Task GetVaultItems_ZeroUserId_ReturnsFail()
        {
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.GetVaultItemsAsync(0);

            Assert.False(result.Success);
            Assert.Equal("Invalid user ID.", result.Message);
        }

        [Fact]
        public async Task GetVaultItems_NegativeUserId_ReturnsFail()
        {
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.GetVaultItemsAsync(-1);

            Assert.False(result.Success);
            Assert.Equal("Invalid user ID.", result.Message);
        }

        // ── DeleteVaultItemAsync — happy path ───────────────────────────────────

        [Fact]
        public async Task DeleteVaultItem_ValidIds_ReturnsSuccess()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.DeleteVaultItemAsync(42, 1)).ReturnsAsync(true);
            var manager = new VaultItemManager(repoMock.Object);

            var result = await manager.DeleteVaultItemAsync(42, 1);

            Assert.True(result.Success);
            Assert.Equal("Vault item deleted successfully.", result.Message);
        }

        // ── DeleteVaultItemAsync — validation failures ──────────────────────────

        [Fact]
        public async Task DeleteVaultItem_ZeroItemId_ReturnsFail()
        {
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.DeleteVaultItemAsync(0, 1);

            Assert.False(result.Success);
            Assert.Equal("Item ID is required.", result.Message);
        }

        [Fact]
        public async Task DeleteVaultItem_NegativeItemId_ReturnsFail()
        {
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.DeleteVaultItemAsync(-5, 1);

            Assert.False(result.Success);
            Assert.Equal("Item ID is required.", result.Message);
        }

        [Fact]
        public async Task DeleteVaultItem_ZeroUserId_ReturnsFail()
        {
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.DeleteVaultItemAsync(1, 0);

            Assert.False(result.Success);
            Assert.Equal("Invalid user ID.", result.Message);
        }

        [Fact]
        public async Task DeleteVaultItem_NegativeUserId_ReturnsFail()
        {
            var manager = new VaultItemManager(new Mock<IVaultItemRepository>().Object);

            var result = await manager.DeleteVaultItemAsync(1, -1);

            Assert.False(result.Success);
            Assert.Equal("Invalid user ID.", result.Message);
        }

        // ── DeleteVaultItemAsync — repository failures ──────────────────────────

        [Fact]
        public async Task DeleteVaultItem_RepositoryReturnsFalse_ReturnsNotFound()
        {
            var repoMock = new Mock<IVaultItemRepository>();
            repoMock.Setup(r => r.DeleteVaultItemAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);
            var manager = new VaultItemManager(repoMock.Object);

            var result = await manager.DeleteVaultItemAsync(999, 1);

            Assert.False(result.Success);
            Assert.Equal("Item not found or access denied.", result.Message);
        }
    }
}
