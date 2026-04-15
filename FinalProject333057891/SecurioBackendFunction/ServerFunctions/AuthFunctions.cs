using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using SecurioModels.DataTransferObjects;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SecurioBackendFunction.ServerFunctions;

public class AuthFunctions
{
    private readonly AuthManager _authManager;

    public AuthFunctions(AuthManager authManager)
    {
        _authManager = authManager;
    }

    // Handles the Register HTTP request and catches any unexpected errors.
    [Function("RegisterUser")]
    public async Task<IActionResult> Register([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            var signup = JsonConvert.DeserializeObject<User>(await new StreamReader(req.Body).ReadToEndAsync());
            var result = await _authManager.RegisterAsync(signup);
            return result.Success ? new OkObjectResult(result) : new ConflictObjectResult(result);
        }
        catch (Exception ex)
        {
            // This catch ensures the client ALWAYS gets a JSON BaseResponse, never a raw crash string.
            return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = "An internal error occurred." });
        }
    }

    // Handles the Login HTTP request and ensures a secure JSON response.
    [Function("VerifyLogin")]
    public async Task<IActionResult> Login([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            var attempt = JsonConvert.DeserializeObject<User>(await new StreamReader(req.Body).ReadToEndAsync());
            var result = await _authManager.VerifyLoginAsync(attempt.Email, attempt.MasterPasswordKey);
            return result.Success ? new OkObjectResult(result) : new UnauthorizedObjectResult(result);
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = "An internal error occurred." });
        }
    }
}

