using Android.Content;
using Android.Util;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using System.Threading.Tasks;

namespace SecurioClient
{
    /// <summary>Static helper that performs the password-health check and fires local notifications for any problems found.</summary>
    public static class PasswordCheckWorker
    {
        private const string Tag = "PasswordCheckWorker";
        private const string DefaultUsername = "User";

        /// <summary>Runs the password-health check and posts notifications for each category that has issues.</summary>
        public static async Task RunCheckAsync(Context context)
        {
            // Retrieve the persisted user ID from secure storage (survives logout).
            int userId = await StorageHelper.GetUserId();
            if (userId <= 0)
            {
                Log.Info(Tag, "No stored user ID — skipping password check.");
                return;
            }

            var data = await PasswordCheckService.FetchAsync(userId);

            if (data == null)
            {
                Log.Warn(Tag, "Server check failed.");
                return;
            }

            // Retrieve stored username for personalised notification text.
            string username = await StorageHelper.GetUsername() ?? DefaultUsername;

            // Fire individual notifications for each category that has issues.
            if (data.BreachedCount > 0)
            {
                NotificationHelper.Show(
                    context,
                    NotificationHelper.NotificationIdBreach,
                    "⚠️ Breached Passwords Detected",
                    $"{username}, {data.BreachedCount} of your passwords appeared in known data breaches. Change them now.");
            }

            if (data.OldCount > 0)
            {
                NotificationHelper.Show(
                    context,
                    NotificationHelper.NotificationIdOld,
                    "🕐 Old Passwords Detected",
                    $"{username}, {data.OldCount} of your passwords haven't been updated in over 90 days.");
            }

            if (data.MasterPasswordOld)
            {
                NotificationHelper.Show(
                    context,
                    NotificationHelper.NotificationIdMaster,
                    "🔑 Master Password Needs Update",
                    $"{username}, your master password hasn't been changed in over 90 days. Consider updating it.");
            }

            Log.Info(Tag, $"Check done — breached={data.BreachedCount}, old={data.OldCount}, masterOld={data.MasterPasswordOld}.");
        }
    }
}
