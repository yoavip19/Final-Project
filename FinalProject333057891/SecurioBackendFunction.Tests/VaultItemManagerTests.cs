using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;

namespace SecurioBackendFunction.Tests
{
    // Unit tests for VaultItemManager: AddVaultItemAsync, UpdateVaultItemAsync,
    // GetVaultItemsAsync, DeleteVaultItemAsync — including HIBP integration.
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

        // ── AddVaultItemAsync: happy path ────────────────────────────────────────

        [Fact]
        public async Task Add_ValidItem_ReturnsSuccess()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(42);

            var result = await new VaultItemManager(repo.Object).AddVaultItemAsync(ValidItem());

            Assert.True(result.Success);
            Assert.Equal("Vault item added successfully.", result.Message);
        }

        [Fact]
        public async Task Add_ValidItem_SetsIdOnReturnedData()
        {
            const int newId = 99;
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(newId);

            var result = await new VaultItemManager(repo.Object).AddVaultItemAsync(ValidItem());

            Assert.Equal(newId, result.Data!.Id);
        }

        [Fact]
        public async Task Add_ValidItem_SetsLastUpdateOnReturnedData()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(1);

            var before = DateTime.UtcNow.AddSeconds(-1);
            var result = await new VaultItemManager(repo.Object).AddVaultItemAsync(ValidItem());
            var after  = DateTime.UtcNow.AddSeconds(1);

            Assert.InRange(result.Data!.LastUpdate, before, after);
        }

        [Fact]
        public async Task Add_OptionalFieldsNull_StillSucceeds()
        {
            var item = ValidItem();
            item.AccountUsername = null;
            item.Notes = null;

            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(5);

            var result = await new VaultItemManager(repo.Object).AddVaultItemAsync(item);

            Assert.True(result.Success);
        }

        // ── AddVaultItemAsync: HIBP integration ──────────────────────────────────

        [Fact]
        public async Task Add_PwnedPassword_SetsIsLeakedTrue()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(true);

            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(1);

            var result = await new VaultItemManager(repo.Object, hibp.Object).AddVaultItemAsync(ValidItem());

            Assert.True(result.Data!.IsLeaked);
        }

        [Fact]
        public async Task Add_SafePassword_SetsIsLeakedFalse()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(false);

            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(1);

            var result = await new VaultItemManager(repo.Object, hibp.Object).AddVaultItemAsync(ValidItem());

            Assert.False(result.Data!.IsLeaked);
        }

        [Fact]
        public async Task Add_NoHibpService_IsLeakedRemainsDefault()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(1);

            // No hibp service — should not throw and IsLeaked stays false.
            var result = await new VaultItemManager(repo.Object).AddVaultItemAsync(ValidItem());

            Assert.True(result.Success);
            Assert.False(result.Data!.IsLeaked);
        }

        // ── AddVaultItemAsync: validation failures ────────────────────────────────

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Add_MissingAccountName_ReturnsFail(string? accountName)
        {
            var item = ValidItem();
            item.AccountName = accountName!;

            var result = await new VaultItemManager(new Mock<IVaultItemRepository>().Object)
                .AddVaultItemAsync(item);

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
            var result = await new VaultItemManager(new Mock<IVaultItemRepository>().Object)
                .AddVaultItemAsync(item);
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
            var result = await new VaultItemManager(new Mock<IVaultItemRepository>().Object)
                .AddVaultItemAsync(item);
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
            var result = await new VaultItemManager(new Mock<IVaultItemRepository>().Object)
                .AddVaultItemAsync(item);
            Assert.False(result.Success);
            Assert.Equal("Tag is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Add_MissingSha1Hash_ReturnsFail(string? sha1)
        {
            var item = ValidItem();
            item.Sha1Hash = sha1!;
            var result = await new VaultItemManager(new Mock<IVaultItemRepository>().Object)
                .AddVaultItemAsync(item);
            Assert.False(result.Success);
            Assert.Equal("Sha1Hash is required.", result.Message);
        }

        // ── AddVaultItemAsync: database errors ────────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Add_RepositoryReturnsNonPositiveId_ReturnsDatabaseError(int returnedId)
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.AddVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(returnedId);

            var result = await new VaultItemManager(repo.Object).AddVaultItemAsync(ValidItem());

            Assert.False(result.Success);
            Assert.Equal("Database error.", result.Message);
        }

        // ── UpdateVaultItemAsync: happy path ─────────────────────────────────────

        [Fact]
        public async Task Update_ValidItem_ReturnsSuccess()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id = 42;

            var result = await new VaultItemManager(repo.Object).UpdateVaultItemAsync(item);

            Assert.True(result.Success);
            Assert.Equal("Vault item updated successfully.", result.Message);
            Assert.Equal(42, result.Data!.Id);
        }

        [Fact]
        public async Task Update_PasswordChanged_SetsLastUpdate()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id = 1;
            item.PasswordChanged = true;

            var before = DateTime.UtcNow.AddSeconds(-1);
            var result = await new VaultItemManager(repo.Object).UpdateVaultItemAsync(item);
            var after  = DateTime.UtcNow.AddSeconds(1);

            Assert.InRange(result.Data!.LastUpdate, before, after);
        }

        [Fact]
        public async Task Update_PasswordNotChanged_DoesNotSetLastUpdate()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var original = DateTime.MinValue;
            var item = ValidItem();
            item.Id = 1;
            item.PasswordChanged = false;
            item.LastUpdate = original;

            var result = await new VaultItemManager(repo.Object).UpdateVaultItemAsync(item);

            // LastUpdate not touched because password didn't change.
            Assert.Equal(original, result.Data!.LastUpdate);
        }

        // ── UpdateVaultItemAsync: HIBP integration ────────────────────────────────

        [Fact]
        public async Task Update_PasswordChanged_PwnedHash_SetsIsLeakedTrue()
        {
            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(It.IsAny<string>())).ReturnsAsync(true);

            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id = 1;
            item.PasswordChanged = true;

            var result = await new VaultItemManager(repo.Object, hibp.Object).UpdateVaultItemAsync(item);

            Assert.True(result.Data!.IsLeaked);
        }

        [Fact]
        public async Task Update_PasswordNotChanged_HibpNotCalled()
        {
            var hibp = new Mock<IHibpService>();
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var item = ValidItem();
            item.Id = 1;
            item.PasswordChanged = false;

            await new VaultItemManager(repo.Object, hibp.Object).UpdateVaultItemAsync(item);

            hibp.Verify(h => h.IsPasswordPwnedAsync(It.IsAny<string>()), Times.Never);
        }

        // ── UpdateVaultItemAsync: validation failures ─────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Update_InvalidId_ReturnsFail(int id)
        {
            var item = ValidItem();
            item.Id = id;
            var result = await new VaultItemManager(new Mock<IVaultItemRepository>().Object)
                .UpdateVaultItemAsync(item);
            Assert.False(result.Success);
            Assert.Equal("Item ID is required.", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Update_MissingAccountName_ReturnsFail(string? name)
        {
            var item = ValidItem();
            item.Id = 1;
            item.AccountName = name!;
            var result = await new VaultItemManager(new Mock<IVaultItemRepository>().Object)
                .UpdateVaultItemAsync(item);
            Assert.False(result.Success);
            Assert.Equal("Account name is required.", result.Message);
        }

        // ── UpdateVaultItemAsync: repository failures ─────────────────────────────

        [Fact]
        public async Task Update_RepositoryReturnsFalse_ReturnsNotFound()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.UpdateVaultItemAsync(It.IsAny<VaultItem>())).ReturnsAsync(false);

            var item = ValidItem();
            item.Id = 999;
            var result = await new VaultItemManager(repo.Object).UpdateVaultItemAsync(item);

            Assert.False(result.Success);
            Assert.Equal("Item not found or access denied.", result.Message);
        }

        // ── GetVaultItemsAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task Get_ValidUserId_ReturnsAllItems()
        {
            var items = new List<VaultItem> { ValidItem(), ValidItem() };
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(items);

            var result = await new VaultItemManager(repo.Object).GetVaultItemsAsync(1);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);
        }

        [Fact]
        public async Task Get_NoItems_ReturnsEmptyList()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());

            var result = await new VaultItemManager(repo.Object).GetVaultItemsAsync(1);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Get_InvalidUserId_ReturnsFail(int userId)
        {
            var result = await new VaultItemManager(new Mock<IVaultItemRepository>().Object)
                .GetVaultItemsAsync(userId);
            Assert.False(result.Success);
            Assert.Equal("Invalid user ID.", result.Message);
        }

        // ── DeleteVaultItemAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task Delete_ValidIds_ReturnsSuccess()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.DeleteVaultItemAsync(42, 1)).ReturnsAsync(true);

            var result = await new VaultItemManager(repo.Object).DeleteVaultItemAsync(42, 1);

            Assert.True(result.Success);
            Assert.Equal("Vault item deleted successfully.", result.Message);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        public async Task Delete_InvalidItemId_ReturnsFail(int itemId, int userId)
        {
            var result = await new VaultItemManager(new Mock<IVaultItemRepository>().Object)
                .DeleteVaultItemAsync(itemId, userId);
            Assert.False(result.Success);
            Assert.Equal("Item ID is required.", result.Message);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        public async Task Delete_InvalidUserId_ReturnsFail(int itemId, int userId)
        {
            var result = await new VaultItemManager(new Mock<IVaultItemRepository>().Object)
                .DeleteVaultItemAsync(itemId, userId);
            Assert.False(result.Success);
            Assert.Equal("Invalid user ID.", result.Message);
        }

        [Fact]
        public async Task Delete_RepositoryReturnsFalse_ReturnsNotFound()
        {
            var repo = new Mock<IVaultItemRepository>();
            repo.Setup(r => r.DeleteVaultItemAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);

            var result = await new VaultItemManager(repo.Object).DeleteVaultItemAsync(999, 1);

            Assert.False(result.Success);
            Assert.Equal("Item not found or access denied.", result.Message);
        }
    }
}
