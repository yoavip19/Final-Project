using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SecurioBackendFunction.ServerFunctions
{
    // Manages HTTP endpoints for vault-item operations.
    public class VaultItemFunctions
    {
        private readonly VaultItemManager _vaultItemManager;

        public VaultItemFunctions(VaultItemManager vaultItemManager)
        {
            _vaultItemManager = vaultItemManager;
        }

        // Receives an encrypted vault item from the client, validates the session, and stores it.
        [Function("AddVaultItem")]
        public async Task<IActionResult> AddVaultItem(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                // Extract the Bearer token from the Authorization header
                var authHeader = req.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return new UnauthorizedObjectResult(
                        new ServerResponse<VaultItem> { Success = false, Message = "Unauthorized." });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var principal = JwtHelper.ValidateToken(token);

                if (principal == null)
                {
                    return new UnauthorizedObjectResult(
                        new ServerResponse<VaultItem> { Success = false, Message = "Unauthorized." });
                }

                int userId = JwtHelper.GetUserIdFromPrincipal(principal);
                if (userId <= 0)
                {
                    return new UnauthorizedObjectResult(
                        new ServerResponse<VaultItem> { Success = false, Message = "Unauthorized." });
                }

                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var item = JsonConvert.DeserializeObject<VaultItem>(body);

                // Bind the authenticated user's ID so the client cannot spoof ownership.
                item.UserId = userId;

                var result = await _vaultItemManager.AddVaultItemAsync(item);
                return result.Success
                    ? new OkObjectResult(result)
                    : new BadRequestObjectResult(result);
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(
                    new ServerResponse<VaultItem> { Success = false, Message = "An internal error occurred." });
            }
        }
    }
}
