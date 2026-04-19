using Android.App;
using Android.Content;
using Android.OS;

namespace SecurioClient.Helpers
{
    // Manages local notification delivery for the periodic password-health check.
    // No external service (Firebase) is used; notifications are posted directly
    // through the Android NotificationManager.
    public static class NotificationHelper
    {
        private const string ChannelId = "securio_password_check";
        private const string ChannelName = "Password Health Alerts";

        // Notification IDs — each category gets a stable ID so repeated checks
        // replace the previous notification instead of stacking duplicates.
        public const int NotificationIdBreach = 1001;
        public const int NotificationIdOld = 1002;
        public const int NotificationIdMaster = 1003;

        // Creates the notification channel required on Android 8.0+.
        // Safe to call multiple times — the system ignores duplicate channel creation.
        public static void CreateChannel(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    ChannelId,
                    ChannelName,
                    NotificationImportance.High)
                {
                    Description = "Alerts when stored passwords are breached, old, or need rotation."
                };

                var manager = (NotificationManager)context.GetSystemService(Context.NotificationService);
                manager?.CreateNotificationChannel(channel);
            }
        }

        // Posts a local notification with the given title, body, and stable ID.
        public static void Show(Context context, int notificationId, string title, string body)
        {
            CreateChannel(context);

            var builder = new Notification.Builder(context, ChannelId)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true);

            var manager = (NotificationManager)context.GetSystemService(Context.NotificationService);
            manager?.Notify(notificationId, builder.Build());
        }
    }
}
