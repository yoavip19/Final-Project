using Moq;
using Xunit;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels.DataTransferObjects;

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
    }
}
