using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioBackendFunction.ServerFunctions;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using Xunit;

namespace SecurioBackendFunction.Tests
{
    // Integration-style tests for the PasswordCheckFunctions HTTP endpoint.
    // Exercises the full HTTP → PasswordCheckManager → (mocked) Repository chain.
    // Covers: valid request, missing UserId, zero UserId, user not found, and all
    // issue combinations in the response payload.
    // To run: dotnet test SecurioBackendFunction.Tests/SecurioBackendFunction.Tests.csproj
    public class PasswordCheckFunctionsTests
    {
        private static HttpRequest BuildRequest(object? body)
        {
            var mock   = new Mock<HttpRequest>();
            var json   = body != null ? JsonConvert.SerializeObject(body) : string.Empty;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            mock.Setup(r => r.Body).Returns(stream);
            mock.Setup(r => r.Headers).Returns(new HeaderDictionary());
            return mock.Object;
        }

        private static PasswordCheckFunctions BuildFunctions(
            Mock<IUserRepository>? userRepo  = null,
            Mock<IVaultItemRepository>? vault = null)
        {
            userRepo ??= new Mock<IUserRepository>();
            vault    ??= new Mock<IVaultItemRepository>();
            return new PasswordCheckFunctions(new PasswordCheckManager(userRepo.Object, vault.Object));
        }

        private static User FreshUser() => new User
        {
            Id                 = 1,
            LastPasswordUpdate = DateTime.UtcNow.AddDays(-10)
        };

        private static User OldMasterUser() => new User
        {
            Id                 = 1,
            LastPasswordUpdate = DateTime.UtcNow.AddDays(-100)
        };

        // ── Missing / invalid body ────────────────────────────────────────────────

        [Fact]
        public async Task EmptyBody_Returns400()
        {
            var req    = BuildRequest(null);
            var result = await BuildFunctions().PasswordCheck(req);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ZeroUserId_Returns400()
        {
            var req    = BuildRequest(new PasswordCheckRequest { UserId = 0 });
            var result = await BuildFunctions().PasswordCheck(req);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task NegativeUserId_Returns400()
        {
            var req    = BuildRequest(new PasswordCheckRequest { UserId = -5 });
            var result = await BuildFunctions().PasswordCheck(req);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── User not found ────────────────────────────────────────────────────────

        [Fact]
        public async Task UserNotFound_Returns404()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(99)).ReturnsAsync((User?)null);
            var vault = new Mock<IVaultItemRepository>();
            vault.Setup(r => r.GetVaultItemsByUserIdAsync(99)).ReturnsAsync(new List<VaultItem>());

            var req    = BuildRequest(new PasswordCheckRequest { UserId = 99 });
            var result = await BuildFunctions(userRepo, vault).PasswordCheck(req);

            Assert.IsType<NotFoundObjectResult>(result);
            var body = ((NotFoundObjectResult)result).Value as ServerResponse<PasswordCheckResult>;
            Assert.NotNull(body);
            Assert.False(body!.Success);
        }

        // ── Happy path — no issues ────────────────────────────────────────────────

        [Fact]
        public async Task AllClear_Returns200WithZeroCounters()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            var vault = new Mock<IVaultItemRepository>();
            vault.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());

            var req    = BuildRequest(new PasswordCheckRequest { UserId = 1 });
            var result = await BuildFunctions(userRepo, vault).PasswordCheck(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = (ServerResponse<PasswordCheckResult>)ok.Value!;
            Assert.True(body.Success);
            Assert.Equal(0, body.Data.BreachedCount);
            Assert.Equal(0, body.Data.OldCount);
            Assert.False(body.Data.MasterPasswordOld);
        }

        // ── Breached passwords ────────────────────────────────────────────────────

        [Fact]
        public async Task TwoLeakedItems_BreachedCountIs2()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            var vault = new Mock<IVaultItemRepository>();
            vault.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>
            {
                new VaultItem { IsLeaked = true,  LastUpdate = DateTime.UtcNow.AddDays(-1) },
                new VaultItem { IsLeaked = true,  LastUpdate = DateTime.UtcNow.AddDays(-1) },
                new VaultItem { IsLeaked = false, LastUpdate = DateTime.UtcNow.AddDays(-1) }
            });

            var req    = BuildRequest(new PasswordCheckRequest { UserId = 1 });
            var result = await BuildFunctions(userRepo, vault).PasswordCheck(req);

            var ok   = Assert.IsType<OkObjectResult>(result);
            var body = (ServerResponse<PasswordCheckResult>)ok.Value!;
            Assert.Equal(2, body.Data.BreachedCount);
        }

        // ── Old passwords ─────────────────────────────────────────────────────────

        [Fact]
        public async Task OneOldItem_OldCountIs1()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            var vault = new Mock<IVaultItemRepository>();
            vault.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>
            {
                new VaultItem { IsLeaked = false, LastUpdate = DateTime.UtcNow.AddDays(-100) },
                new VaultItem { IsLeaked = false, LastUpdate = DateTime.UtcNow.AddDays(-1) }
            });

            var req    = BuildRequest(new PasswordCheckRequest { UserId = 1 });
            var result = await BuildFunctions(userRepo, vault).PasswordCheck(req);

            var ok   = Assert.IsType<OkObjectResult>(result);
            var body = (ServerResponse<PasswordCheckResult>)ok.Value!;
            Assert.Equal(1, body.Data.OldCount);
        }

        // ── Old master password ───────────────────────────────────────────────────

        [Fact]
        public async Task OldMasterPassword_MasterPasswordOldIsTrue()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(OldMasterUser());
            var vault = new Mock<IVaultItemRepository>();
            vault.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());

            var req    = BuildRequest(new PasswordCheckRequest { UserId = 1 });
            var result = await BuildFunctions(userRepo, vault).PasswordCheck(req);

            var ok   = Assert.IsType<OkObjectResult>(result);
            var body = (ServerResponse<PasswordCheckResult>)ok.Value!;
            Assert.True(body.Data.MasterPasswordOld);
        }

        // ── All issues present ────────────────────────────────────────────────────

        [Fact]
        public async Task AllIssues_AllFieldsPopulated()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(OldMasterUser());
            var vault = new Mock<IVaultItemRepository>();
            vault.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>
            {
                new VaultItem { IsLeaked = true,  LastUpdate = DateTime.UtcNow.AddDays(-95) },
                new VaultItem { IsLeaked = false, LastUpdate = DateTime.UtcNow.AddDays(-91) }
            });

            var req    = BuildRequest(new PasswordCheckRequest { UserId = 1 });
            var result = await BuildFunctions(userRepo, vault).PasswordCheck(req);

            var ok   = Assert.IsType<OkObjectResult>(result);
            var body = (ServerResponse<PasswordCheckResult>)ok.Value!;
            Assert.True(body.Success);
            Assert.Equal(1, body.Data.BreachedCount);
            Assert.Equal(2, body.Data.OldCount);
            Assert.True(body.Data.MasterPasswordOld);
        }

        // ── Response structure ────────────────────────────────────────────────────

        [Fact]
        public async Task SuccessResponse_HasSuccessTrueAndNonNullData()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(FreshUser());
            var vault = new Mock<IVaultItemRepository>();
            vault.Setup(r => r.GetVaultItemsByUserIdAsync(1)).ReturnsAsync(new List<VaultItem>());

            var req    = BuildRequest(new PasswordCheckRequest { UserId = 1 });
            var result = await BuildFunctions(userRepo, vault).PasswordCheck(req);

            var ok   = Assert.IsType<OkObjectResult>(result);
            var body = ok.Value as ServerResponse<PasswordCheckResult>;
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotNull(body.Data);
        }

        [Fact]
        public async Task ErrorResponse_HasSuccessFalseAndMessage()
        {
            var req    = BuildRequest(new PasswordCheckRequest { UserId = 0 });
            var result = await BuildFunctions().PasswordCheck(req);

            var bad  = Assert.IsType<BadRequestObjectResult>(result);
            var body = bad.Value as ServerResponse<PasswordCheckResult>;
            Assert.NotNull(body);
            Assert.False(body!.Success);
            Assert.False(string.IsNullOrEmpty(body.Message));
        }
    }
}
