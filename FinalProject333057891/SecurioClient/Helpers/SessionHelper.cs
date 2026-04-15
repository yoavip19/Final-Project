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

namespace SecurioClient.Helpers
{
    // A volatile, memory-only manager that holds the active vault encryption key and cleared-out cache during a user session.
    public static class SessionHelper
    {
        // The AES key derived from the Master Password; kept in RAM and never saved to disk.
        public static string SessionVaultKey { get; private set; }

        // Cached list of encrypted vault items to avoid repeated server calls during a session.
        ///public static List<VaultItem> CachedVault { get; set; } = new List<VaultItem>();

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
            ///CachedVault.Clear();

            GC.Collect();
        }
    }
}