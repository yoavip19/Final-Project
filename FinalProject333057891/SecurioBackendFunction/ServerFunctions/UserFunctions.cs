using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SecurioBackendFunction.Logic;
using SecurioModels;
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
        [Function("GetProfile")]
        public async Task<IActionResult> GetProfile(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            try
            {
                // In a production app, the ID should come from the JWT claims for security
                if (!int.TryParse(req.Query["userId"], out int userId))
                {
                    return new BadRequestObjectResult(new BaseResponse<User> { Success = false, Message = "Invalid User ID." });
                }

                var result = await _userManager.GetProfileAsync(userId);
                return new OkObjectResult(result);
            }
            catch
            {
                return new BadRequestObjectResult(new BaseResponse<User> { Success = false, Message = "Error loading profile data." });
            }
        }
    }
}
