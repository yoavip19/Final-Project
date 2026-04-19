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
    // Exposes the periodic password-health check endpoint used by the client's background worker.
    public class PasswordCheckFunctions
    {
        private readonly PasswordCheckManager _manager;

        public PasswordCheckFunctions(PasswordCheckManager manager)
        {
            _manager = manager;
        }

        // Accepts a POST with { UserId } and returns breach / old / master-old counts.
        // No JWT required — the background worker may run when the token is expired.
        // The endpoint only returns aggregate counts, not actual passwords, so
        // exposing it by user ID is acceptable for this use-case.
        [Function("PasswordCheck")]
        public async Task<IActionResult> PasswordCheck(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<PasswordCheckRequest>(body);

                if (request == null || request.UserId <= 0)
                {
                    return new BadRequestObjectResult(
                        new ServerResponse<PasswordCheckResult>
                        {
                            Success = false,
                            Message = "UserId is required."
                        });
                }

                var result = await _manager.CheckAsync(request.UserId);
                return result.Success
                    ? new OkObjectResult(result)
                    : new BadRequestObjectResult(result);
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(
                    new ServerResponse<PasswordCheckResult>
                    {
                        Success = false,
                        Message = "An internal error occurred."
                    });
            }
        }
    }

    // Request body for the PasswordCheck endpoint.
    internal sealed class PasswordCheckRequest
    {
        public int UserId { get; set; }
    }
}
