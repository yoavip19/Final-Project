using Android.App;
using Android.Content;
using Android.OS;
using SecurioModels.DataTransferObjects;

namespace SecurioClient.Helpers
{
    /// <summary>Handles all Android-specific notification operations for the password-monitor service.</summary>
    public static class NotificationHelper
    {
        // Notification channel for background password-health alerts.
        public const string ChannelId   = "securio_monitor_channel";
        public const string ChannelName = "Password Monitor";

        // Notification IDs
        public const int AlertNotificationId   = 1001;
        public const int ForegroundNotificationId = 1002;

        /// <summary>Creates the notification channel required on Android 8+.</summary>
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

        /// <summary>Posts a local alert notification with the given message text.</summary>
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

        /// <summary>Builds the persistent foreground-service notification shown while the PasswordMonitorService is running.</summary>
        public static Notification BuildForegroundNotification(Context context)
        {
            return new Notification.Builder(context, ChannelId)
                .SetContentTitle("Securio")
                .SetContentText("Monitoring your passwords in the background")
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetOngoing(true)
                .Build();
        }

        /// <summary>Returns true when the user has not disabled notifications for this app (covers both the POST_NOTIFICATIONS runtime permission and the per-app toggle in Settings).</summary>
        public static bool AreNotificationsEnabled(Context context)
        {
            var nm = (NotificationManager)context.GetSystemService(Context.NotificationService);
            return nm?.AreNotificationsEnabled() ?? false;
        }

        /// <summary>Evaluates the PasswordCheckResult and posts an alert only when there is at least one issue to report.</summary>
        public static void PostCheckResult(Context context, PasswordCheckResult result)
        {
            if (!PasswordCheckDecision.ShouldNotify(result)) return;
            PostAlert(context, PasswordCheckDecision.BuildMessage(result));
        }
    }
}
