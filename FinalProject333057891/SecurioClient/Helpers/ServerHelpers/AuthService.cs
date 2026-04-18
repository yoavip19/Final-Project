using SecurioClient.Helpers;
using SecurioClient;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using SecurioModels.DataTransferObjects;

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

            // 2. Derive the vault key and start the in-memory session
            string vaultKey = EncryptionHelper.DeriveKey(password, salt);
            SessionHelper.StartSession(vaultKey);

            // 3. Persist the vault key so it can be restored after an app restart
            await StorageHelper.SaveVaultKey(vaultKey);

            // 4. Fetch the user's vault items and cache them in memory
            await FetchAndCacheVaultAsync();

            // 5. Compute password-health warnings and cache them for the session
            SessionHelper.CachedWarnings = WarningsHelper.ComputeWarnings(
                SessionHelper.CachedVault, vaultKey);
        }

        // Fetches the user's vault items from the server and stores them in SessionHelper.CachedVault.
        public static async Task FetchAndCacheVaultAsync()
        {
            var vaultService = new VaultService();
            var vaultResult = await vaultService.GetVaultItemsAsync();
            if (vaultResult.Success && vaultResult.Data != null)
                SessionHelper.CachedVault = vaultResult.Data;
            else
                SessionHelper.CachedVault = new List<VaultItem>();
        }

        // Asks the server to verify that the stored JWT is still valid and unexpired.
        // Returns true if the server accepts the token; false if it is missing, invalid, or expired.
        public async Task<bool> ValidateTokenAsync()
        {
            var result = await GetAsync<object>("ValidateToken");
            return result.Success;
        }
    }
}
