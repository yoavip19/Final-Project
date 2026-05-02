using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Logic;
using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SecurioBackendFunction.ServerFunctions;

/// <summary>Handles HTTP endpoints for user authentication.</summary>
public class AuthFunctions
{
    private readonly AuthManager _authManager;

    /// <summary>Initializes a new instance of AuthFunctions.</summary>
    public AuthFunctions(AuthManager authManager)
    {
        _authManager = authManager;
    }

    /// <summary>Handles the Register HTTP request and catches any unexpected errors.</summary>
    [Function("RegisterUser")]
    public async Task<IActionResult> Register([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
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
            return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = $"An internal error occurred. Error - {ex.Message} " });
        }
    }

    /// <summary>Handles the Login HTTP request and ensures a secure JSON response.</summary>
    [Function("VerifyLogin")]
    public async Task<IActionResult> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
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

    /// <summary>Gets the user's salts for the login process.</summary>
    [Function("GetSalts")]
    public async Task<IActionResult> GetSalts([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            var request = JsonConvert.DeserializeObject<dynamic>(await new StreamReader(req.Body).ReadToEndAsync());
            string email = request.Email;

            var user = await _authManager.GetUserSaltsAsync(email);
            return user.Success ? new OkObjectResult(user) : new NotFoundObjectResult(user);
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = "An internal error occurred." });
        }
    }

    /// <summary>Validates an existing JWT token; returns 200 if valid and unexpired, 401 otherwise.</summary>
    [Function("ValidateToken")]
    public IActionResult ValidateToken([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            var authHeader = req.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });

            var token = authHeader.Substring("Bearer ".Length);
            var principal = JwtHelper.ValidateToken(token);

            if (principal == null)
                return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Token is invalid or expired." });

            int userId = JwtHelper.GetUserIdFromPrincipal(principal);
            return new OkObjectResult(new ServerResponse<object> { Success = true, Message = "Token is valid.", Data = new { UserId = userId } });
        }
        catch (Exception)
        {
            return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = "An internal error occurred." });
        }
    }
}

