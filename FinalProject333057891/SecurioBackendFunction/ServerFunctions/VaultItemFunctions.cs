using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SecurioBackendFunction.ServerFunctions
{
    /// <summary>Manages HTTP endpoints for vault-item operations.</summary>
    public class VaultItemFunctions
    {
        private readonly VaultItemManager _vaultItemManager;

        /// <summary>Initializes a new instance of VaultItemFunctions.</summary>
        public VaultItemFunctions(VaultItemManager vaultItemManager)
        {
            _vaultItemManager = vaultItemManager;
        }

        /// <summary>Receives an encrypted vault item from the client, validates the session, and stores it.</summary>
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

                if (item == null)
                {
                    return new BadRequestObjectResult(
                        new ServerResponse<VaultItem> { Success = false, Message = "Invalid request body." });
                }

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

        /// <summary>Receives an updated vault item from the client, validates the session, and persists the changes.</summary>
        [Function("UpdateVaultItem")]
        public async Task<IActionResult> UpdateVaultItem(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
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

                if (item == null)
                {
                    return new BadRequestObjectResult(
                        new ServerResponse<VaultItem> { Success = false, Message = "Invalid request body." });
                }

                // Bind the authenticated user's ID so the client cannot spoof ownership.
                item.UserId = userId;

                var result = await _vaultItemManager.UpdateVaultItemAsync(item);
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

        /// <summary>Returns all vault items for the authenticated user.</summary>
        [Function("GetVaultItems")]
        public async Task<IActionResult> GetVaultItems(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            try
            {
                var authHeader = req.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return new UnauthorizedObjectResult(
                        new ServerResponse<List<VaultItem>> { Success = false, Message = "Unauthorized." });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var principal = JwtHelper.ValidateToken(token);

                if (principal == null)
                {
                    return new UnauthorizedObjectResult(
                        new ServerResponse<List<VaultItem>> { Success = false, Message = "Unauthorized." });
                }

                int userId = JwtHelper.GetUserIdFromPrincipal(principal);
                if (userId <= 0)
                {
                    return new UnauthorizedObjectResult(
                        new ServerResponse<List<VaultItem>> { Success = false, Message = "Unauthorized." });
                }

                var result = await _vaultItemManager.GetVaultItemsAsync(userId);
                return result.Success
                    ? new OkObjectResult(result)
                    : new BadRequestObjectResult(result);
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(
                    new ServerResponse<List<VaultItem>> { Success = false, Message = "An internal error occurred." });
            }
        }

        /// <summary>Permanently deletes a vault item owned by the authenticated user.</summary>
        [Function("DeleteVaultItem")]
        public async Task<IActionResult> DeleteVaultItem(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                var authHeader = req.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return new UnauthorizedObjectResult(
                        new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var principal = JwtHelper.ValidateToken(token);

                if (principal == null)
                {
                    return new UnauthorizedObjectResult(
                        new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                int userId = JwtHelper.GetUserIdFromPrincipal(principal);
                if (userId <= 0)
                {
                    return new UnauthorizedObjectResult(
                        new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<DeleteVaultItemRequest>(body);

                if (request == null || request.Id <= 0)
                {
                    return new BadRequestObjectResult(
                        new ServerResponse<object> { Success = false, Message = "Item ID is required." });
                }

                var result = await _vaultItemManager.DeleteVaultItemAsync(request.Id, userId);
                return result.Success
                    ? new OkObjectResult(result)
                    : new BadRequestObjectResult(result);
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(
                    new ServerResponse<object> { Success = false, Message = "An internal error occurred." });
            }
        }
    }

    /// <summary>Request body for the DeleteVaultItem endpoint.</summary>
    internal sealed class DeleteVaultItemRequest
    {
        public int Id { get; set; }
    }
}
