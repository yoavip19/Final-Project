using Android.App;
using Android.Content;
using Android.OS;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using System.Threading.Tasks;

namespace SecurioClient
{
    /// <summary>Foreground service that performs a password-health check once every 24 hours.</summary>
    [Service(Exported = false)]
    public class PasswordMonitorService : Service
    {
        private const long IntervalMs = 10000; //10 seconds //24 * 60 * 60 * 1000L; // 24 hours

        private Android.OS.Handler _handler;
        private Java.Lang.Runnable _runnable;

        /// <summary>Creates the notification channel, starts the foreground notification, and schedules the first check.</summary>
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

        /// <summary>Returns START_STICKY so Android restarts the service if it is killed.</summary>
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            // START_STICKY ensures Android restarts the service if it is killed.
            return StartCommandResult.Sticky;
        }

        /// <summary>Not a bound service; returns null.</summary>
        public override IBinder OnBind(Intent intent) => null;

        /// <summary>Removes pending callbacks and cleans up when the service is destroyed.</summary>
        public override void OnDestroy()
        {
            _handler?.RemoveCallbacks(_runnable);
            base.OnDestroy();
        }

        /// <summary>Runs the password-check once and then re-schedules itself for the next interval.</summary>
        private async Task RunCycleAsync()
        {
            await PerformCheckAsync(this);
            _handler.PostDelayed(_runnable, IntervalMs);
        }

        /// <summary>Performs a single password-health check and posts a notification when issues are found.</summary>
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
