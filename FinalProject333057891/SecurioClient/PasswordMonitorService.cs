using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using System;

namespace SecurioClient
{
    // A foreground service that survives the user swiping the app from recents.
    // It runs the password-health check on a 24-hour loop and shows a low-priority
    // persistent notification so Android keeps the process alive.  Started by
    // MainActivity on first launch and by BootReceiver on every subsequent device reboot.
    [Service(Exported = false)]
    public class PasswordMonitorService : Service
    {
        private const string Tag = "PasswordMonitorService";
        private const int ForegroundNotificationId = 9001;
        private const string ForegroundChannelId = "securio_monitor";
        private const string ForegroundChannelName = "Securio Background Monitor";
        private const long IntervalMs = 24L * 60 * 60 * 1000; // 24 hours

        private Handler _handler;
        private Java.Lang.Runnable _checkRunnable;

        public override void OnCreate()
        {
            base.OnCreate();
            _handler = new Handler(Looper.MainLooper);
            _checkRunnable = new Java.Lang.Runnable(async () =>
            {
                try
                {
                    await PasswordCheckWorker.RunCheckAsync(ApplicationContext);
                }
                catch (Exception ex)
                {
                    Log.Error(Tag, $"Periodic check failed: {ex.Message}");
                }
                // Schedule the next iteration only if the service is still alive.
                _handler?.PostDelayed(_checkRunnable, IntervalMs);
            });
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            StartForeground(ForegroundNotificationId, BuildForegroundNotification());

            // Remove any pending callback so restarting the service (e.g. after reboot) doesn't
            // schedule a second overlapping chain.
            _handler.RemoveCallbacks(_checkRunnable);
            _handler.Post(_checkRunnable);

            Log.Info(Tag, "Service started; first check posted immediately.");
            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent) => null;

        public override void OnDestroy()
        {
            _handler?.RemoveCallbacks(_checkRunnable);
            base.OnDestroy();
        }

        private Notification BuildForegroundNotification()
        {
            CreateForegroundChannel();
            return new Notification.Builder(this, ForegroundChannelId)
                .SetContentTitle("🔒 Securio")
                .SetContentText("Protecting your passwords in the background.")
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetOngoing(true)
                .Build();
        }

        private void CreateForegroundChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    ForegroundChannelId,
                    ForegroundChannelName,
                    NotificationImportance.Low)
                {
                    Description = "Shows while Securio monitors your password health in the background."
                };

                var manager = (NotificationManager)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(channel);
            }
        }
    }
}
