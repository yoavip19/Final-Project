using Android.App;
using Android.Content;
using Android.OS;
using SecurioModels.DataTransferObjects;

namespace SecurioClient.Helpers
{
    // Handles all Android-specific notification operations for the password-monitor service.
    // The notification-criteria logic (ShouldNotify / BuildMessage) lives in the
    // platform-independent PasswordCheckDecision helper so it can be unit-tested.
    public static class NotificationHelper
    {
        // Notification channel for background password-health alerts.
        public const string ChannelId   = "securio_monitor_channel";
        public const string ChannelName = "Password Monitor";

        // Notification IDs
        public const int AlertNotificationId   = 1001;
        public const int ForegroundNotificationId = 1002;

        // Creates the notification channel (required on Android 8+).
        public static void CreateChannel(Context context)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

            var channel = new NotificationChannel(
                ChannelId,
                ChannelName,
                NotificationImportance.Default)
            {
                Description = "Securio password-health background alerts"
            };

            var nm = (NotificationManager)context.GetSystemService(Context.NotificationService);
            nm?.CreateNotificationChannel(channel);
        }

        // Posts a local alert notification with the given message text.
        public static void PostAlert(Context context, string message)
        {
            var builder = new Notification.Builder(context, ChannelId)
                .SetContentTitle("Securio Password Alert")
                .SetContentText(message)
                .SetSmallIcon(Android.Resource.Drawable.IcDialogAlert)
                .SetAutoCancel(true);

            var nm = (NotificationManager)context.GetSystemService(Context.NotificationService);
            nm?.Notify(AlertNotificationId, builder.Build());
        }

        // Builds the persistent foreground-service notification shown while the
        // PasswordMonitorService is running (required for foreground services).
        public static Notification BuildForegroundNotification(Context context)
        {
            return new Notification.Builder(context, ChannelId)
                .SetContentTitle("Securio")
                .SetContentText("Monitoring your passwords in the background")
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetOngoing(true)
                .Build();
        }

        // Convenience wrapper: evaluates the PasswordCheckResult and posts an alert
        // only when there is at least one issue to report.
        public static void PostCheckResult(Context context, PasswordCheckResult result)
        {
            if (!PasswordCheckDecision.ShouldNotify(result)) return;
            PostAlert(context, PasswordCheckDecision.BuildMessage(result));
        }
    }
}
