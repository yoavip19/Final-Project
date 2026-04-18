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

using SecurioModels;
using SecurioModels.DataTransferObjects;

namespace SecurioClient.Helpers
{
    // A volatile, memory-only manager that holds the active vault encryption key and cached vault data during a user session.
    public static class SessionHelper
    {
        // The AES key derived from the Master Password; kept in RAM and never saved to disk.
        public static string SessionVaultKey { get; private set; }

        // Cached list of vault items for the authenticated user. Kept in RAM only.
        // This is the single source of truth for the vault list during a session.
        public static List<VaultItem> CachedVault { get; set; } = new List<VaultItem>();

        // Cached password-risk warning counters.  Computed at login and
        // invalidated (set to null) whenever the vault contents change.
        public static WarningsData CachedWarnings { get; set; }

        // Indicates if the session is currently active with a valid key.
        public static bool IsAuthenticated => !string.IsNullOrEmpty(SessionVaultKey);

        // Starts a session by storing the derived AES key in memory.
        public static void StartSession(string aesKey)
        {
            SessionVaultKey = aesKey;
        }

        // Ends the session by wiping the AES key and cached data from memory.
        public static void EndSession()
        {
            SessionVaultKey = null;
            CachedVault.Clear();
            CachedWarnings = null;

            GC.Collect();
        }

        // Clears the cached warnings so they will be recomputed on the next access.
        public static void InvalidateWarnings()
        {
            CachedWarnings = null;
        }

        // Adds a vault item to the in-memory cache.
        public static void AddVaultItem(VaultItem item)
        {
            if (item != null)
                CachedVault.Add(item);
        }

        // Updates an existing vault item in the in-memory cache by matching its Id.
        public static void UpdateVaultItem(VaultItem updatedItem)
        {
            if (updatedItem == null) return;

            var existing = CachedVault.FirstOrDefault(v => v.Id == updatedItem.Id);
            if (existing != null)
            {
                existing.AccountName     = updatedItem.AccountName;
                existing.AccountUsername  = updatedItem.AccountUsername;
                existing.IV              = updatedItem.IV;
                existing.Tag             = updatedItem.Tag;
                existing.CipherText      = updatedItem.CipherText;
                existing.Notes           = updatedItem.Notes;
                existing.Sha1Hash        = updatedItem.Sha1Hash;
                existing.IsLeaked        = updatedItem.IsLeaked;
                existing.LastUpdate      = updatedItem.LastUpdate;
            }
        }

        // Removes a vault item from the in-memory cache by Id.
        public static void RemoveVaultItem(int itemId)
        {
            CachedVault.RemoveAll(v => v.Id == itemId);
        }
    }
}