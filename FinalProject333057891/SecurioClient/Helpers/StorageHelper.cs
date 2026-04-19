using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;
using System.Threading.Tasks;
using Java.Awt.Font;
using SecurioModels.DataTransferObjects;

namespace SecurioClient.Helpers
{
    // Manages the persistent, hardware-encrypted storage of the user's ID, username, and authentication token on the device.
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

        // Saves the user's unique ID to secure storage, ensuring it is encrypted and protected by the device's hardware security features.
        public static async Task SaveUserId(int id)
        {
            await SecureStorage.SetAsync(KeyUserId, id.ToString());
        }

        // Retrieves the user's unique ID from secure storage, returning 0 if not found or invalid.
        public static async Task<int> GetUserId()
        {
            var id = await SecureStorage.GetAsync(KeyUserId);
            return int.TryParse(id, out int result) ? result : 0;
        }

        // Persists the user's display name to the device's secure storage.
        public static async Task SaveUsername(string name)
        {
            await SecureStorage.SetAsync(KeyUsername, name);
        }

        // Retrieves the stored display name for UI personalization.
        public static async Task<string> GetUsername()
        {
            return await SecureStorage.GetAsync(KeyUsername);
        }

        // Saves the JSON Web Token (JWT) to secure storage to maintain the user's authenticated session, ensuring it is encrypted and protected by the device's hardware security features.
        public static async Task SaveJwt(string token)
        {
            await SecureStorage.SetAsync(KeyJwt, token);
        }

        // Retrieves the JSON Web Token (JWT) from secure storage, returning null if not found.
        public static async Task<string> GetJwt()
        {
            return await SecureStorage.GetAsync(KeyJwt);
        }

        // Saves the derived AES vault key to secure storage so the session can be restored after an app restart.
        public static async Task SaveVaultKey(string vaultKey)
        {
            await SecureStorage.SetAsync(KeyVaultKey, vaultKey);
        }

        // Retrieves the stored AES vault key, returning null if not present.
        public static async Task<string> GetVaultKey()
        {
            return await SecureStorage.GetAsync(KeyVaultKey);
        }

        // Persists the full user profile, including history timestamps, to secure local storage.
        public static async Task SaveProfileAsync(User profile)
        {
            await SecureStorage.SetAsync(KeyEmail, profile.Email);
            await SecureStorage.SetAsync(KeyPasswordCount, profile.PasswordCount.ToString());
            await SecureStorage.SetAsync(KeyCreatedAt, profile.CreatedAt.ToString("o"));
            await SecureStorage.SetAsync(KeyLastLogin, profile.LastLogin.ToString("o"));
            await SecureStorage.SetAsync(KeyLastPasswordChange, profile.LastPasswordUpdate.ToString("o"));
        }

        // Retrieves the fully populated user profile from the local cache.
        public static async Task<User> GetCachedProfileAsync()
        {
            var email = await SecureStorage.GetAsync(KeyEmail);
            if (string.IsNullOrEmpty(email)) return null;

            return new User
            {
                Id = await GetUserId(),
                Username = await GetUsername(),
                Email = email,
                PasswordCount = int.TryParse(await SecureStorage.GetAsync(KeyPasswordCount), out var cnt) ? cnt : 0,
                CreatedAt = DateTime.TryParse(await SecureStorage.GetAsync(KeyCreatedAt), out var createdAt) ? createdAt : DateTime.MinValue,
                LastLogin = DateTime.TryParse(await SecureStorage.GetAsync(KeyLastLogin), out var lastLogin) ? lastLogin : DateTime.MinValue,
                LastPasswordUpdate = DateTime.TryParse(await SecureStorage.GetAsync(KeyLastPasswordChange), out var lastPwChange) ? lastPwChange : DateTime.MinValue
            };
        }

        // Clears session-sensitive data from secure storage while preserving the
        // user ID and username. These two values are kept so the background
        // PasswordCheckWorker can still poll the server after logout.
        // Items are removed individually to avoid a window where preserved values
        // are temporarily absent (which would happen with RemoveAll + restore).
        public static Task ClearSessionAsync()
        {
            SecureStorage.Remove(KeyJwt);
            SecureStorage.Remove(KeyVaultKey);
            SecureStorage.Remove(KeyEmail);
            SecureStorage.Remove(KeyCreatedAt);
            SecureStorage.Remove(KeyPasswordCount);
            SecureStorage.Remove(KeyLastLogin);
            SecureStorage.Remove(KeyLastPasswordChange);
            return Task.CompletedTask;
        }

        // Clears ALL data from secure storage, including the user ID and username.
        // Use this only when the account is being deleted.
        public static void ClearAll()
        {
            SecureStorage.RemoveAll();
        }
    }
}