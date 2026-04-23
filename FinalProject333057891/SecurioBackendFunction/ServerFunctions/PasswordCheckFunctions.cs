using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using SecurioBackendFunction.Logic;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SecurioBackendFunction.ServerFunctions
{
    /// <summary>Exposes the password-health check as an unauthenticated HTTP endpoint.</summary>
    public class PasswordCheckFunctions
    {
        private readonly PasswordCheckManager _manager;

        /// <summary>Initializes a new instance of PasswordCheckFunctions.</summary>
        public PasswordCheckFunctions(PasswordCheckManager manager)
        {
            _manager = manager;
        }

        /// <summary>Runs the password-health check for the specified user.</summary>
        [Function("PasswordCheck")]
        public async Task<IActionResult> PasswordCheck(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                var body    = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<PasswordCheckRequest>(body);

                if (request == null || request.UserId <= 0)
                {
                    return new BadRequestObjectResult(new ServerResponse<PasswordCheckResult>
                        {
                            Success = false,
                            Message = "UserId is required."
                        });
                }

                var result = await _manager.GetPasswordCheckAsync(request.UserId);

                if (result == null)
                {
                    return new NotFoundObjectResult(new ServerResponse<PasswordCheckResult>
                    {
                        Success = false,
                        Message = "User not found."
                    });
            }

                return new OkObjectResult(new ServerResponse<PasswordCheckResult>
                {
                    Success = true,
                    Data    = result
                });
            }
            catch
            {
                return new BadRequestObjectResult(new ServerResponse<PasswordCheckResult>
                    {
                        Success = false,
                    Message = "Error running password check."
                    });
            }
        }
    }
}
