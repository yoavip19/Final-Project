using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
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
    }
}
