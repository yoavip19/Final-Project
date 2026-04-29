using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Logic
{
    /// <summary>Manages authentication logic for user registration and login.</summary>
    public class AuthManager
    {
        private readonly IUserRepository _repo;

        /// <summary>Initializes a new instance of AuthManager.</summary>
        public AuthManager(IUserRepository repo)
        {
            _repo = repo;
        }

        /// <summary>Registers user and generates a token immediately for a seamless UI transition.</summary>
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

        /// <summary>Validates login credentials and returns a session token.</summary>
        public async Task<ServerResponse<AuthData>> VerifyLoginAsync(string email, string key)
        {
            var user = await _repo.GetUserByEmailAsync(email);
            if (user == null || user.MasterPasswordKey != key)
                return new ServerResponse<AuthData> { Success = false, Message = "Invalid email or password." };

            string token = Helpers.JwtHelper.GenerateJwtToken(user.Id, user.Username);
            await _repo.UpdateLastLoginAsync(user.Id);
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

        /// <summary>Retrieves the user's salts for the login process.</summary>
        public async Task<ServerResponse<SaltData>> GetUserSaltsAsync(string email)
        {
            var user = await _repo.GetUserByEmailAsync(email);
            if (user == null)
                return new ServerResponse<SaltData> { Success = false, Message = "User not found." };

            return new ServerResponse<SaltData>
            {
                Success = true,
                Message = "Salts retrieved successfully",
                Data = new SaltData
                {
                    AuthSalt = user.AuthSalt,
                    EncryptionSalt = user.EncryptionSalt
                }
            };
        }
    }
}

