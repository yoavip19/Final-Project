using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Logic
{
    public class AuthManager
    {
        private readonly UserRepository _repo;
        public AuthManager(UserRepository repo) => _repo = repo;

        // Registers user and generates a token immediately for a seamless UI transition.
        public async Task<ServerResponse<AuthData>> RegisterAsync(User user)
        {
            if (await _repo.EmailExistsAsync(user.Email))
                return new ServerResponse<AuthData> { Success = false, Message = "Email already registered." };

            int newId = await _repo.CreateUserAsync(user);
            if (newId <= 0) return new ServerResponse<AuthData> { Success = false, Message = "Database error." };

            string token = Helpers.JwtHelper.GenerateJwtToken(newId, user.Username);
            return new ServerResponse<AuthData>
            {
                Success = true,
                Message = "User registered successfully",
                Data = new AuthData
                {
                    UserId = newId,
                    Username = user.Username,
                    Token = token
                }
            };
        }

        // Validates login credentials and returns a session token.
        public async Task<ServerResponse<AuthData>> VerifyLoginAsync(string email, string key)
        {
            var user = await _repo.GetUserByEmailAsync(email);
            if (user == null || user.MasterPasswordKey != key)
                return new ServerResponse<AuthData> { Success = false, Message = "Invalid email or password." };

            string token = Helpers.JwtHelper.GenerateJwtToken(user.Id, user.Username);
            return new ServerResponse<AuthData>
            {
                Success = true,
                Message = "Login successful",
                Data = new AuthData
                {
                    Token = token,
                    UserId = user.Id,
                    Username = user.Username
                }
            };
        }
    }
}

