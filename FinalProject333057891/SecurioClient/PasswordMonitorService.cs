using Android.App;
using Android.Content;
using Android.OS;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using System.Threading.Tasks;

namespace SecurioClient
{
    /// <summary>Foreground service that performs a password-health check once every 24 hours
    /// and registers a screen-off receiver for emergency clipboard purging.</summary>
    [Service(Exported = false)]
    public class PasswordMonitorService : Service
    {
        private const long IntervalMs = 24 * 60 * 60 * 1000L; // 24 hours

        private Android.OS.Handler _handler;
        private Java.Lang.Runnable _runnable;
        private ScreenOffReceiver _screenOffReceiver;

        /// <summary>Creates the notification channel, starts the foreground notification, schedules the first check,
        /// and registers the screen-off receiver for emergency clipboard clearing.</summary>
        public override void OnCreate()
        {
            base.OnCreate();

            NotificationHelper.CreateChannel(this);

            // Show the mandatory persistent notification for foreground services.
            StartForeground(
                NotificationHelper.ForegroundNotificationId,
                NotificationHelper.BuildForegroundNotification(this));

            _handler  = new Android.OS.Handler(Looper.MainLooper);
            _runnable = new Java.Lang.Runnable(async () => await RunCycleAsync());

            // Run the first check immediately, then repeat every 24 h.
            _handler.Post(_runnable);

            // Register the screen-off receiver dynamically (ACTION_SCREEN_OFF cannot be
            // declared in the manifest; it must be registered at runtime).
            _screenOffReceiver = new ScreenOffReceiver();
            RegisterReceiver(_screenOffReceiver, new IntentFilter(Intent.ActionScreenOff));
        }

        /// <summary>Returns START_STICKY so Android restarts the service if it is killed.</summary>
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            // START_STICKY ensures Android restarts the service if it is killed.
            return StartCommandResult.Sticky;
        }

        /// <summary>Not a bound service; returns null.</summary>
        public override IBinder OnBind(Intent intent) => null;

        /// <summary>Removes pending callbacks, unregisters the screen-off receiver, and cleans up when the service is destroyed.</summary>
        public override void OnDestroy()
        {
            _handler?.RemoveCallbacks(_runnable);
            if (_screenOffReceiver != null)
            {
                try { UnregisterReceiver(_screenOffReceiver); }
                catch { /* receiver was never registered or already unregistered */ }
                _screenOffReceiver = null;
            }
            base.OnDestroy();
        }

        /// <summary>Runs the password-check once and then re-schedules itself for the next interval.</summary>
        private async Task RunCycleAsync()
        {
            // Only hit the server and post a notification when the user has notifications enabled.
            // If the user toggled notifications off in Settings the check is skipped, but the
            // service keeps running so that notifications resume automatically the next cycle
            // when the user turns them back on — without needing to open the app.
            if (NotificationHelper.AreNotificationsEnabled(this))
                await PerformCheckAsync(this);

            _handler.PostDelayed(_runnable, IntervalMs);
        }

        /// <summary>Performs a single password-health check and posts a notification when issues are found.</summary>
        internal static async Task PerformCheckAsync(Context context)
        {
            int userId = await StorageHelper.GetUserId();
            if (userId <= 0) return;

            var result = await PasswordCheckService.FetchAsync(userId);
            if (result == null) return;

            NotificationHelper.PostCheckResult(context, result);
        }

        /// <summary>
        /// BroadcastReceiver that listens for <c>ACTION_SCREEN_OFF</c> and immediately
        /// purges the clipboard so sensitive credentials are not accessible while the
        /// device is unattended or in a pocket.
        /// </summary>
        private sealed class ScreenOffReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if (intent?.Action == Intent.ActionScreenOff)
                    ClipboardGuard.ClearNow(context);
            }
        }
    }
}
