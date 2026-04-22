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
    // Unit tests for the PasswordCheck HTTP endpoint (POST "PasswordCheck").
    // No JWT is required — caller passes UserId in the request body.
    public class PasswordCheckFunctionsTests
    {
        private static PasswordCheckFunctions Build(
            Mock<IVaultItemRepository> vaultRepo,
            Mock<IUserRepository> userRepo,
            Mock<IHibpService> hibp)
            => new PasswordCheckFunctions(
                new PasswordCheckManager(vaultRepo.Object, userRepo.Object, hibp.Object));

        private static (Mock<IVaultItemRepository>, Mock<IUserRepository>, Mock<IHibpService>) EmptyMocks()
        {
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<VaultItem>());

            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(It.IsAny<int>()))
                .ReturnsAsync(new User { LastPasswordUpdate = DateTime.UtcNow });

            var hibp = new Mock<IHibpService>();
            return (vaultRepo, userRepo, hibp);
        }

        // ── Bad input ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task PasswordCheck_EmptyBody_Returns400()
        {
            var (vaultRepo, userRepo, hibp) = EmptyMocks();
            var result = await Build(vaultRepo, userRepo, hibp)
                .PasswordCheck(HttpTestHelpers.BuildRequest(null, null));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task PasswordCheck_InvalidUserId_Returns400(int userId)
        {
            var (vaultRepo, userRepo, hibp) = EmptyMocks();
            var result = await Build(vaultRepo, userRepo, hibp)
                .PasswordCheck(HttpTestHelpers.BuildRequest(null, new { UserId = userId }));
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ServerResponse<PasswordCheckResult>>(bad.Value);
            Assert.False(response.Success);
        }

        // ── Happy path ────────────────────────────────────────────────────────────

        [Fact]
        public async Task PasswordCheck_ValidUserId_Returns200()
        {
            var (vaultRepo, userRepo, hibp) = EmptyMocks();
            var result = await Build(vaultRepo, userRepo, hibp)
                .PasswordCheck(HttpTestHelpers.BuildRequest(null, new { UserId = 1 }));
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task PasswordCheck_ValidUserId_ResponseContainsResult()
        {
            var (vaultRepo, userRepo, hibp) = EmptyMocks();
            var result = await Build(vaultRepo, userRepo, hibp)
                .PasswordCheck(HttpTestHelpers.BuildRequest(null, new { UserId = 1 }));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<PasswordCheckResult>>(ok.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
        }

        [Fact]
        public async Task PasswordCheck_EmptyVault_ReturnsZeroCounts()
        {
            var (vaultRepo, userRepo, hibp) = EmptyMocks();
            var result = await Build(vaultRepo, userRepo, hibp)
                .PasswordCheck(HttpTestHelpers.BuildRequest(null, new { UserId = 1 }));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<PasswordCheckResult>>(ok.Value);
            Assert.Equal(0, response.Data!.BreachedCount);
            Assert.Equal(0, response.Data.OldCount);
            Assert.False(response.Data.MasterPasswordOld);
        }

        // ── Counts are reflected in response ─────────────────────────────────────

        [Fact]
        public async Task PasswordCheck_BreachedItem_BreachedCountIsOne()
        {
            const string sha1 = "5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8";
            var vaultRepo = new Mock<IVaultItemRepository>();
            vaultRepo.Setup(r => r.GetVaultItemsByUserIdAsync(1))
                .ReturnsAsync(new List<VaultItem>
                {
                    new VaultItem { Sha1Hash = sha1, LastUpdate = DateTime.UtcNow }
                });

            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1))
                .ReturnsAsync(new User { LastPasswordUpdate = DateTime.UtcNow });

            var hibp = new Mock<IHibpService>();
            hibp.Setup(h => h.IsPasswordPwnedAsync(sha1)).ReturnsAsync(true);

            var result = await Build(vaultRepo, userRepo, hibp)
                .PasswordCheck(HttpTestHelpers.BuildRequest(null, new { UserId = 1 }));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<PasswordCheckResult>>(ok.Value);
            Assert.Equal(1, response.Data!.BreachedCount);
        }

        [Fact]
        public async Task PasswordCheck_OldMasterPassword_MasterOldIsTrue()
        {
            var (vaultRepo, userRepo, hibp) = EmptyMocks();
            userRepo.Setup(r => r.GetUserProfileAsync(1))
                .ReturnsAsync(new User { LastPasswordUpdate = DateTime.UtcNow.AddDays(-100) });

            var result = await Build(vaultRepo, userRepo, hibp)
                .PasswordCheck(HttpTestHelpers.BuildRequest(null, new { UserId = 1 }));
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ServerResponse<PasswordCheckResult>>(ok.Value);
            Assert.True(response.Data!.MasterPasswordOld);
        }
    }
}
