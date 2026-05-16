using System;
using System.Collections.Generic;
using Xamarin.Essentials;
using System.Threading.Tasks;
using SecurioModels.DataTransferObjects;

namespace SecurioClient.Helpers
{
    /// <summary>Manages the persistent, hardware-encrypted storage of the user's ID, username, and authentication token on the device.</summary>
    public static class StorageHelper
    {
        // These keys identify the data in the encrypted file
        private const string KeyUserId = "user_id";
        private const string KeyUsername = "user_name";
        private const string KeyJwt = "jwt_token";
        private const string KeyVaultKey = "vault_key";
        private const string KeyEmail = "email";
        private const string KeyCreatedAt = "created_at";
        private const string KeyPasswordCount = "password_count";
        private const string KeyLastLogin = "last_login";
        private const string KeyLastPasswordChange = "last_password_change";

        /// <summary>Saves the user's unique ID to secure storage.</summary>
        public static async Task SaveUserIdAsync(int id)
        {
            await SecureStorage.SetAsync(KeyUserId, id.ToString());
        }

        /// <summary>Retrieves the user's unique ID from secure storage, returning 0 if not found or invalid.</summary>
        public static async Task<int> GetUserIdAsync()
        {
            var id = await SecureStorage.GetAsync(KeyUserId);
            return int.TryParse(id, out int result) ? result : 0;
        }

        /// <summary>Persists the user's display name to the device's secure storage.</summary>
        public static async Task SaveUsernameAsync(string name)
        {
            await SecureStorage.SetAsync(KeyUsername, name);
        }

        /// <summary>Retrieves the stored display name for UI personalization.</summary>
        public static async Task<string> GetUsernameAsync()
        {
            return await SecureStorage.GetAsync(KeyUsername);
        }

        /// <summary>Saves the JSON Web Token to secure storage to maintain the user's authenticated session.</summary>
        public static async Task SaveJwtAsync(string token)
        {
            await SecureStorage.SetAsync(KeyJwt, token);
        }

        /// <summary>Retrieves the JSON Web Token from secure storage, returning null if not found.</summary>
        public static async Task<string> GetJwtAsync()
        {
            return await SecureStorage.GetAsync(KeyJwt);
        }

        /// <summary>Saves the derived AES vault key to secure storage so the session can be restored after an app restart.</summary>
        public static async Task SaveVaultKeyAsync(string vaultKey)
        {
            await SecureStorage.SetAsync(KeyVaultKey, vaultKey);
        }

        /// <summary>Retrieves the stored AES vault key, returning null if not present.</summary>
        public static async Task<string> GetVaultKeyAsync()
        {
            return await SecureStorage.GetAsync(KeyVaultKey);
        }

        /// <summary>Persists the full user profile including history timestamps to secure local storage.</summary>
        public static async Task SaveProfileAsync(User profile)
        {
            await SecureStorage.SetAsync(KeyEmail, profile.Email);
            await SecureStorage.SetAsync(KeyPasswordCount, profile.PasswordCount.ToString());
            await SecureStorage.SetAsync(KeyCreatedAt, profile.CreatedAt.ToString("o"));
            await SecureStorage.SetAsync(KeyLastLogin, profile.LastLogin.ToString("o"));
            await SecureStorage.SetAsync(KeyLastPasswordChange, profile.LastPasswordUpdate.ToString("o"));
        }

        /// <summary>Retrieves the fully populated user profile from the local cache.</summary>
        public static async Task<User> GetCachedProfileAsync()
        {
            var email = await SecureStorage.GetAsync(KeyEmail);
            if (string.IsNullOrEmpty(email)) return null;

            return new User
            {
                Id = await GetUserIdAsync(),
                Username = await GetUsernameAsync(),
                Email = email,
                PasswordCount = int.TryParse(await SecureStorage.GetAsync(KeyPasswordCount), out var cnt) ? cnt : 0,
                CreatedAt = DateTime.TryParse(await SecureStorage.GetAsync(KeyCreatedAt), out var createdAt) ? createdAt : DateTime.MinValue,
                LastLogin = DateTime.TryParse(await SecureStorage.GetAsync(KeyLastLogin), out var lastLogin) ? lastLogin : DateTime.MinValue,
                LastPasswordUpdate = DateTime.TryParse(await SecureStorage.GetAsync(KeyLastPasswordChange), out var lastPwChange) ? lastPwChange : DateTime.MinValue
            };
        }

        /// <summary>Clears session-sensitive data from secure storage while preserving user ID and username.</summary>
        public static void ClearSession()
        {
            SecureStorage.Remove(KeyJwt);
            SecureStorage.Remove(KeyVaultKey);
            SecureStorage.Remove(KeyEmail);
            SecureStorage.Remove(KeyCreatedAt);
            SecureStorage.Remove(KeyPasswordCount);
            SecureStorage.Remove(KeyLastLogin);
            SecureStorage.Remove(KeyLastPasswordChange);
        }

        /// <summary>Clears ALL data from secure storage including user ID and username; use only when deleting the account.</summary>
        public static void ClearAll()
        {
            SecureStorage.RemoveAll();
        }
    }
}
