using Android.Content;
using AndroidX.AppCompat.App;

namespace SecurioClient.Helpers
{
    /// <summary>
    /// Shared helper for bottom-navigation tab switching.
    /// Centralises the mapping of tab name → activity so that each host activity
    /// only needs a single call instead of duplicating a switch/if chain.
    /// </summary>
    public static class BottomNavHelper
    {
        /// <summary>
        /// Navigates from <paramref name="activity"/> to the activity that corresponds
        /// to <paramref name="tab"/>.  If <paramref name="tab"/> equals
        /// <paramref name="currentTab"/> the call is a no-op (user re-tapped the
        /// active tab).  The current activity is finished so the back-stack does not
        /// accumulate stacked instances of the same main screens.
        /// </summary>
        public static void Navigate(AppCompatActivity activity, string tab, string currentTab)
        {
            if (tab == currentTab)
                return;

            Intent intent = tab switch
            {
                "vault"    => new Intent(activity, typeof(VaultActivity)),
                "warnings" => new Intent(activity, typeof(WarningsActivity)),
                "profile"  => new Intent(activity, typeof(ProfileActivity)),
                _          => null
            };

            if (intent != null)
            {
                activity.StartActivity(intent);
                activity.Finish();
            }
        }
    }
}
