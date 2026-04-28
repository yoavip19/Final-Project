using Moq;
using Xunit;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;
using System.Collections.Generic;

namespace SecurioBackendFunction.Tests
{
    // Comprehensive unit tests for VaultItemManager.
    // Covers Add, Update, Get, and Delete operations including the client-provided IsLeaked flag.
    // All repository dependencies are mocked.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class VaultItemManagerTests
    {
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

        private static VaultItemManager Build(
            Mock<IVaultItemRepository>? repo = null)
        {
            repo ??= new Mock<IVaultItemRepository>();
            return new VaultItemManager(repo.Object);
        }

        // ── AddVaultItemAsync: happy path ────────────────────────────────────────

        [Fact]
        public async Task Add_ValidItem_ReturnsSuccess()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(42);

            var result = await Build(repo).AddVaultItemAsync(ValidItem());

            Assert.True(result.Success);
            Assert.Equal("Vault item added successfully.", result.Message);
        }

        [Fact]
        public async Task Add_ValidItem_ReturnedDataContainsAssignedId()
        {
            const int expectedId = 77;
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(expectedId);

            var result = await Build(repo).AddVaultItemAsync(ValidItem());

            Assert.NotNull(result.Data);
            Assert.Equal(expectedId, result.Data!.Id);
        }

        [Fact]
        public async Task Add_ValidItem_LastUpdateIsSet()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(1);

            var result = await Build(repo).AddVaultItemAsync(ValidItem());

            Assert.NotEqual(default, result.Data!.LastUpdate);
        }

        [Fact]
        public async Task Add_NullOptionalFields_StillSucceeds()
        {
            var item = ValidItem();
            item.AccountUsername = null;
            item.Notes           = null;

            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(5);

            var result = await Build(repo).AddVaultItemAsync(item);

            Assert.True(result.Success);
        }

        // ── AddVaultItemAsync: validation failures ───────────────────────────────

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Add_MissingAccountName_ReturnsFail(string? name)
        {
            var item = ValidItem();
            item.AccountName = name!;

            var result = await Build().AddVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Account name is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Add_MissingCipherText_ReturnsFail(string? ct)
        {
            var item = ValidItem();
            item.CipherText = ct!;

            var result = await Build().AddVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("CipherText is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Add_MissingIV_ReturnsFail(string? iv)
        {
            var item = ValidItem();
            item.IV = iv!;

            var result = await Build().AddVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("IV is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Add_MissingTag_ReturnsFail(string? tag)
        {
            var item = ValidItem();
            item.Tag = tag!;

            var result = await Build().AddVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Tag is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Add_MissingHash_ReturnsFail(string? hash)
        {
            var item = ValidItem();
            item.Sha1Hash = hash!;

            var result = await Build().AddVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Sha1Hash is required.", result.Message);
        }

        // ── AddVaultItemAsync: database errors ───────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Add_DatabaseReturnsNonPositiveId_ReturnsDatabaseError(int returnedId)
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(returnedId);

            var result = await Build(repo).AddVaultItemAsync(ValidItem());

            Assert.False(result.Success);
            Assert.Equal("Database error.", result.Message);
        }

        // ── AddVaultItemAsync: IsLeaked flag trust ───────────────────────────────

        [Fact]
        public async Task Add_IsLeakedTrueFromClient_PreservedOnAdd()
        {
            // The server trusts the IsLeaked flag set by the client (HIBP is checked client-side).
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(1);

            var item = ValidItem();
            item.IsLeaked = true;

            var result = await Build(repo).AddVaultItemAsync(item);

            Assert.True(result.Data!.IsLeaked);
        }

        [Fact]
        public async Task Add_IsLeakedFalseFromClient_PreservedOnAdd()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(1);

            var item = ValidItem();
            item.IsLeaked = false;

            var result = await Build(repo).AddVaultItemAsync(item);

            Assert.False(result.Data!.IsLeaked);
        }

        // ── UpdateVaultItemAsync: happy path ─────────────────────────────────────

        [Fact]
        public async Task Update_ValidItem_ReturnsSuccess()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id = 42;
            var result = await Build(repo).UpdateVaultItemAsync(item);

            Assert.True(result.Success);
            Assert.Equal("Vault item updated successfully.", result.Message);
        }

        [Fact]
        public async Task Update_ValidItem_ReturnedDataContainsId()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id = 42;
            var result = await Build(repo).UpdateVaultItemAsync(item);

            Assert.Equal(42, result.Data!.Id);
        }

        [Fact]
        public async Task Update_PasswordChanged_SetsLastUpdate()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id             = 1;
            item.PasswordChanged = true;
            var result = await Build(repo).UpdateVaultItemAsync(item);

            Assert.NotEqual(default, result.Data!.LastUpdate);
        }

        [Fact]
        public async Task Update_PasswordNotChanged_LastUpdateUnchanged()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id             = 1;
            item.PasswordChanged = false;
            item.LastUpdate      = default;
            var result = await Build(repo).UpdateVaultItemAsync(item);

            Assert.Equal(default, result.Data!.LastUpdate);
        }

        // ── UpdateVaultItemAsync: IsLeaked flag trust ────────────────────────────

        [Fact]
        public async Task Update_IsLeakedTrueFromClient_PreservedOnUpdate()
        {
            // The server trusts the IsLeaked flag set by the client — same as on Add.
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id              = 1;
            item.PasswordChanged = true;
            item.IsLeaked        = true;
            var result = await Build(repo).UpdateVaultItemAsync(item);

            Assert.True(result.Data!.IsLeaked);
        }

        // ── UpdateVaultItemAsync: validation failures ────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Update_InvalidItemId_ReturnsFail(int id)
        {
            var item = ValidItem();
            item.Id = id;

            var result = await Build().UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Item ID is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Update_MissingAccountName_ReturnsFail(string? name)
        {
            var item = ValidItem();
            item.Id          = 1;
            item.AccountName = name!;

            var result = await Build().UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Account name is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Update_MissingCipherText_ReturnsFail(string? ct)
        {
            var item = ValidItem();
            item.Id         = 1;
            item.CipherText = ct!;

            var result = await Build().UpdateVaultItemAsync(item);

            Assert.False(result.Success);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Update_MissingIV_ReturnsFail(string? iv)
        {
            var item = ValidItem();
            item.Id = 1;
            item.IV = iv!;

            var result = await Build().UpdateVaultItemAsync(item);

            Assert.False(result.Success);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Update_MissingTag_ReturnsFail(string? tag)
        {
            var item = ValidItem();
            item.Id  = 1;
            item.Tag = tag!;

            var result = await Build().UpdateVaultItemAsync(item);

            Assert.False(result.Success);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Update_MissingHash_ReturnsFail(string? hash)
        {
            var item = ValidItem();
            item.Id       = 1;
            item.Sha1Hash = hash!;

            var result = await Build().UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Sha1Hash is required.", result.Message);
        }

        // ── UpdateVaultItemAsync: repository failure ─────────────────────────────

        [Fact]
        public async Task Update_RepositoryReturnsFalse_ReturnsNotFound()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(false);

            var item = ValidItem();
            item.Id = 999;
            var result = await Build(repo).UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Item not found or access denied.", result.Message);
        }

        // ── GetVaultItemsAsync: happy path ───────────────────────────────────────

        [Fact]
        public async Task GetVaultItems_ValidUserId_ReturnsSuccess()
        {
            var items = new List<VaultItem> { ValidItem(), ValidItem() };
            var repo  = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);

            var result = await Build(repo).GetVaultItemsAsync(1);

            Assert.True(result.Success);
            Assert.Equal("Vault items retrieved successfully.", result.Message);
            Assert.Equal(2, result.Data!.Count);
        }

        [Fact]
        public async Task GetVaultItems_EmptyVault_ReturnsEmptyList()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());

            var result = await Build(repo).GetVaultItemsAsync(1);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
        }

        // ── GetVaultItemsAsync: validation failures ──────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetVaultItems_InvalidUserId_ReturnsFail(int userId)
        {
            var result = await Build().GetVaultItemsAsync(userId);

            Assert.False(result.Success);
            Assert.Equal("Invalid user ID.", result.Message);
        }

        // ── DeleteVaultItemAsync: happy path ─────────────────────────────────────

        [Fact]
        public async Task Delete_ValidIds_ReturnsSuccess()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.DeleteVaultItemAsync(10, 1)).ReturnsAsync(true);

            var result = await Build(repo).DeleteVaultItemAsync(10, 1);

            Assert.True(result.Success);
            Assert.Equal("Vault item deleted successfully.", result.Message);
        }

        // ── DeleteVaultItemAsync: validation failures ────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Delete_InvalidItemId_ReturnsFail(int itemId)
        {
            var result = await Build().DeleteVaultItemAsync(itemId, 1);

            Assert.False(result.Success);
            Assert.Equal("Item ID is required.", result.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Delete_InvalidUserId_ReturnsFail(int userId)
        {
            var result = await Build().DeleteVaultItemAsync(1, userId);

            Assert.False(result.Success);
            Assert.Equal("Invalid user ID.", result.Message);
        }

        // ── DeleteVaultItemAsync: repository failure ─────────────────────────────

        [Fact]
        public async Task Delete_RepositoryReturnsFalse_ReturnsNotFound()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.DeleteVaultItemAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);

            var result = await Build(repo).DeleteVaultItemAsync(999, 1);

            Assert.False(result.Success);
            Assert.Equal("Item not found or access denied.", result.Message);
        }
    }
}
