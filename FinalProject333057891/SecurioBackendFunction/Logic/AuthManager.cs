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
    public class AuthManager
    {
        private readonly IUserRepository _repo;
        private readonly IHibpService _hibp;

        public AuthManager(IUserRepository repo, IHibpService hibp)
        {
            _repo = repo;
            _hibp = hibp;
        }

        // Registers user and generates a token immediately for a seamless UI transition.
        // If a PasswordSha1Hash is provided it is checked against the HIBP Pwned Passwords
        // dataset before the account is created.
        public async Task<ServerResponse<AuthData>> RegisterAsync(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.PasswordSha1Hash))
            {
                bool isPwned = await _hibp.IsPasswordPwnedAsync(user.PasswordSha1Hash);
                if (isPwned)
                    return new ServerResponse<AuthData>
                    {
                        Success = false,
                        Message = "Password has been found in a data breach. Please choose a different password."
                    };
            }

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

        // Retrieves the user's salts for the login process. This is a critical step for secure password handling.
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

