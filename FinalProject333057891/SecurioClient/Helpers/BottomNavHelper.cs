using Android.Content;
using AndroidX.AppCompat.App;
using SecurioClient.Activities;

namespace SecurioClient.Helpers
{
    /// <summary>Shared helper for bottom-navigation tab switching that centralises the mapping of tab name to activity.</summary>
    public static class BottomNavHelper
    {
        /// <summary>Navigates from the current activity to the activity corresponding to the given tab; no-op if tab equals currentTab.</summary>
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
