using Android.App;
using Android.Content;
using Android.OS;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using System.Threading.Tasks;

namespace SecurioClient
{
    // Foreground service that performs a password-health check once every 24 hours.
    // Running as a foreground service (START_STICKY) ensures the check survives the
    // user swiping the app from the recent-apps list.
    //
    // Lifecycle:
    //   1. MainActivity.StartPasswordMonitor() calls StartForegroundService.
    //   2. BootReceiver restarts the service after device reboot or app update.
    //   3. OnCreate posts the required persistent foreground notification and
    //      starts the 24-hour Handler loop.
    [Service(Exported = false)]
    public class PasswordMonitorService : Service
    {
        private const long IntervalMs = 10_000; //10 seconds //24 * 60 * 60 * 1000L; // 24 hours

        private Android.OS.Handler _handler;
        private Java.Lang.Runnable _runnable;

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
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            // START_STICKY ensures Android restarts the service if it is killed.
            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent) => null;

        public override void OnDestroy()
        {
            _handler?.RemoveCallbacks(_runnable);
            base.OnDestroy();
        }

        // Runs the password-check once and then re-schedules itself for 24 hours later.
        private async Task RunCycleAsync()
        {
            await PerformCheckAsync(this);
            _handler.PostDelayed(_runnable, IntervalMs);
        }

        // Performs a single password-health check and posts a notification when issues are found.
        // Extracted as an internal static so it can be exercised in integration tests.
        internal static async Task PerformCheckAsync(Context context)
        {
            int userId = await StorageHelper.GetUserId();
            if (userId <= 0) return;

            var result = await PasswordCheckServerService.FetchAsync(userId);
            if (result == null) return;

            NotificationHelper.PostCheckResult(context, result);
        }
    }
}
