using SecurioClient.Helpers;
using SecurioClient;
using SecurioModels;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using SecurioModels.Responses;
using static Android.Telecom.CallScreeningService;


namespace SecurioClient.Helpers.ServerHelpers
{
    // A specialized service that manages identity-related tasks like registering new accounts and verifying credentials.
    public class AuthService : BaseService
    {
        public async Task<(bool Success, string Message)> RegisterAsync(User newUser)
        {
            // 'result' IS the BaseResponse<AuthData>. 
            var result = await PostAsync<AuthData>("RegisterUser", newUser);

            // One Success, One Message. Clean.
            if (result.Success)
            {
                await SetupAuthenticatedSession(result.Data, newUser.MasterPasswordKey, newUser.EncryptionSalt);
            }

            return (result.Success, result.Message);
        }

        // Validates credentials by first fetching user salts to derive keys locally.
        public async Task<(bool Success, string Message)> LoginAsync(string email, string password)
        {
            // 1. Get Salts (Server sends AuthSalt and EncryptionSalt)
            var saltResult = await PostAsync<SaltData>("GetSalts", new { Email = email });

            if (!saltResult.Success)
                return (false, saltResult.Message);

            // 2. Hash the password locally using the AuthSalt
            string hashedMPK = EncryptionHelper.DeriveKey(password, saltResult.Data.AuthSalt);

            // 3. Authenticate with the hashed key
            var authResult = await PostAsync<AuthData>("VerifyLogin", new
            {
                Email = email,
                MasterPasswordKey = hashedMPK
            });

            if (authResult.Success)
            {
                // 4. Start the session using the EncryptionSalt from step 1
                await SetupAuthenticatedSession(authResult.Data, password, saltResult.Data.EncryptionSalt);
            }

            return (authResult.Success, authResult.Message);
        }

        private async Task SetupAuthenticatedSession(AuthData data, string password, string salt)
        {
            // 1. Save to SecureStorage
            await StorageHelper.SaveUserId(data.UserId);
            await StorageHelper.SaveJwt(data.Token);
            await StorageHelper.SaveUsername(data.Username);

            // 2. Start the Session (Derive Key)
            string vaultKey = EncryptionHelper.DeriveKey(password, salt);
            SessionHelper.StartSession(vaultKey);
        }
    }
}
