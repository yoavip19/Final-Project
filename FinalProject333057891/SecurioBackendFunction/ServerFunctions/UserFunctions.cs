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
using System.Text;
using System.Threading.Tasks;

namespace SecurioBackendFunction.ServerFunctions
{
    // Manages HTTP endpoints for non-authentication user data.
    public class UserFunctions
    {
        private readonly UserManager _userManager;

        public UserFunctions(UserManager userManager)
        {
            _userManager = userManager;
        }

        // Retrieves the user's statistics and profile details via a GET request.
        // The user ID is extracted from the validated JWT in the Authorization header.
        [Function("GetProfile")]
        public async Task<IActionResult> GetProfile(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            try
            {
                // Extract the Bearer token from the Authorization header
                var authHeader = req.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return new UnauthorizedObjectResult(new ServerResponse<User> { Success = false, Message = "Unauthorized." });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var principal = JwtHelper.ValidateToken(token);

                if (principal == null)
                {
                    return new UnauthorizedObjectResult(new ServerResponse<User> { Success = false, Message = "Unauthorized." });
                }

                int userId = JwtHelper.GetUserIdFromPrincipal(principal);
                if (userId <= 0)
                {
                    return new UnauthorizedObjectResult(new ServerResponse<User> { Success = false, Message = "Unauthorized." });
                }

                var result = await _userManager.GetProfileAsync(userId);
                return new OkObjectResult(result);
            }
            catch
            {
                return new BadRequestObjectResult(new ServerResponse<User> { Success = false, Message = "Error loading profile data." });
            }
        }

        // Updates the user's account details (username, email, and optionally master password).
        [Function("UpdateUser")]
        public async Task<IActionResult> UpdateUser(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                var authHeader = req.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var principal = JwtHelper.ValidateToken(token);

                if (principal == null)
                {
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                int userId = JwtHelper.GetUserIdFromPrincipal(principal);
                if (userId <= 0)
                {
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<UpdateAccountRequest>(body);

                if (request == null)
                {
                    return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = "Invalid request body." });
                }

                var updatedUser = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    MasterPasswordKey = request.MasterPasswordKey,
                    AuthSalt = request.AuthSalt,
                    EncryptionSalt = request.EncryptionSalt,
                    PasswordSha1Hash = request.PasswordSha1Hash
                };

                var result = await _userManager.UpdateUserAsync(userId, updatedUser, request.PasswordChanged, request.VaultItems);
                return result.Success
                    ? new OkObjectResult(result)
                    : new BadRequestObjectResult(result);
            }
            catch
            {
                return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = "Error updating account." });
            }
        }

        // Permanently deletes the user account and all associated vault items.
        // The user ID is extracted from the validated JWT in the Authorization header.
        [Function("DeleteUser")]
        public async Task<IActionResult> DeleteUser(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                var authHeader = req.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var principal = JwtHelper.ValidateToken(token);

                if (principal == null)
                {
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                int userId = JwtHelper.GetUserIdFromPrincipal(principal);
                if (userId <= 0)
                {
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                var result = await _userManager.DeleteUserAsync(userId);
                return new OkObjectResult(result);
            }
            catch
            {
                return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = "Error deleting account." });
            }
        }

        // Returns the last 4 master-password history entries for the authenticated user.
        // The client uses these (along with each entry's AuthSalt) to detect password reuse
        // before submitting a new master password.
        [Function("GetPasswordHistory")]
        public async Task<IActionResult> GetPasswordHistory(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            try
            {
                var authHeader = req.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var principal = JwtHelper.ValidateToken(token);

                if (principal == null)
                {
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                int userId = JwtHelper.GetUserIdFromPrincipal(principal);
                if (userId <= 0)
                {
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });
                }

                var result = await _userManager.GetPasswordHistoryAsync(userId);
                return new OkObjectResult(result);
            }
            catch
            {
                return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = "Error loading password history." });
            }
        }
    }
}
