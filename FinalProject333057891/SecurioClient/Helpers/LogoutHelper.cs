using Android.App;
using Android.Content;
using AndroidX.AppCompat.App;
using SecurioClient.Activities;
using System.Threading.Tasks;

namespace SecurioClient.Helpers
{
    /// <summary>Shows logout confirmation and clears the authenticated session when confirmed.</summary>
    public static class LogoutHelper
    {
        /// <summary>Shows the shared logout confirmation dialog.</summary>
        public static void ShowLogoutConfirmation(AppCompatActivity activity)
        {
            new AlertDialog.Builder(activity)
                .SetTitle(Resource.String.profile_logout_confirm_title)
                .SetMessage(Resource.String.profile_logout_confirm_message)
                .SetPositiveButton(Resource.String.profile_logout_confirm_yes, async (sender, e) =>
                {
                    await PerformLogoutAsync(activity);
                })
                .SetNegativeButton(Resource.String.profile_logout_confirm_no, (sender, e) => { })
                .Show();
        }

        /// <summary>Clears session state and returns the user to <see cref="LoginActivity"/>.</summary>
        public static async Task PerformLogoutAsync(Activity activity)
        {
            AppLockManager.CancelAutoLockTimer();
            AppLockManager.Unlock();
            SessionHelper.EndSession();
            await StorageHelper.ClearSessionAsync();

            var intent = new Intent(activity, typeof(LoginActivity));
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            activity.StartActivity(intent);
            activity.Finish();
        }
    }
}
