using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using SecurioModels.DataTransferObjects;
using System.Threading.Tasks;

namespace SecurioClient
{
    /// <summary>
    /// Foreground service that performs a password-health check on a recurring schedule.
    /// The interval is controlled by <see cref="TestConfig.CheckIntervalMs"/>:
    /// 30 seconds on the testing branch, 24 hours in production.
    /// </summary>
    [Service(Exported = false)]
    public class PasswordMonitorService : Service
    {
        /// <summary>The most recent password-check result; null until the first cycle completes.</summary>
        public static PasswordCheckResult LastCheckResult { get; private set; }

        private Android.OS.Handler _handler;
        private Java.Lang.Runnable _runnable;

        /// <summary>Creates the notification channel, starts the foreground notification, and schedules the first check.</summary>
        public override void OnCreate()
        {
            base.OnCreate();

            Log.Debug(TestConfig.LogTag, $"PasswordMonitorService.OnCreate — interval={TestConfig.CheckIntervalMs} ms");

            NotificationHelper.CreateChannel(this);

            // Show the mandatory persistent notification for foreground services.
            StartForeground(
                NotificationHelper.ForegroundNotificationId,
                NotificationHelper.BuildForegroundNotification(this));

            _handler  = new Android.OS.Handler(Looper.MainLooper);
            _runnable = new Java.Lang.Runnable(async () => await RunCycleAsync());

            // Run the first check immediately, then repeat at the configured interval.
            _handler.Post(_runnable);
        }

        /// <summary>Returns START_STICKY so Android restarts the service if it is killed.</summary>
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Log.Debug(TestConfig.LogTag, "PasswordMonitorService.OnStartCommand — START_STICKY");
            // START_STICKY ensures Android restarts the service if it is killed.
            return StartCommandResult.Sticky;
        }

        /// <summary>Not a bound service; returns null.</summary>
        public override IBinder OnBind(Intent intent) => null;

        /// <summary>Removes pending callbacks and cleans up when the service is destroyed.</summary>
        public override void OnDestroy()
        {
            Log.Debug(TestConfig.LogTag, "PasswordMonitorService.OnDestroy");
            _handler?.RemoveCallbacks(_runnable);
            base.OnDestroy();
        }

        /// <summary>Runs the password-check once and then re-schedules itself for the next interval.</summary>
        private async Task RunCycleAsync()
        {
            await PerformCheckAsync(this);
            _handler.PostDelayed(_runnable, TestConfig.CheckIntervalMs);
        }

        /// <summary>Performs a single password-health check and posts a notification when issues are found.</summary>
        internal static async Task PerformCheckAsync(Context context)
        {
            int userId = await StorageHelper.GetUserId();
            if (userId <= 0)
            {
                Log.Debug(TestConfig.LogTag, "PerformCheckAsync — no stored userId, skipping.");
                return;
            }

            Log.Debug(TestConfig.LogTag, $"PerformCheckAsync — calling server for userId={userId}");

            var result = await PasswordCheckServerService.FetchAsync(userId);
            if (result == null)
            {
                Log.Debug(TestConfig.LogTag, "PerformCheckAsync — server returned null (network error or no issues).");
                return;
            }

            // Cache the latest result so TestDashboardActivity (and ADB) can inspect it.
            LastCheckResult = result;

            Log.Debug(TestConfig.LogTag,
                $"PerformCheckAsync — breached={result.BreachedCount}, old={result.OldCount}, masterOld={result.MasterPasswordOld}");

            NotificationHelper.PostCheckResult(context, result);
        }
    }
}
