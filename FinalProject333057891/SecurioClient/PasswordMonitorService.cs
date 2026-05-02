using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using System.Threading.Tasks;

namespace SecurioClient
{
    /// <summary>Foreground service that performs a password-health check once every 24 hours.</summary>
    [Service(Exported = false)]
    public class PasswordMonitorService : Service
    {
        private const string Tag = "PasswordMonitorService";
        private const long IntervalMs  = 24 * 60 * 60 * 1000L; // 24 hours
        private const long MsPerHour   = 3_600_000L;

        private Android.OS.Handler _handler;
        private Java.Lang.Runnable _runnable;

        /// <summary>Creates the notification channel, starts the foreground notification, and schedules the first check.</summary>
        public override void OnCreate()
        {
            base.OnCreate();
            Log.Info(Tag, "Service created — starting foreground and scheduling first check");

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
            Log.Info(Tag, $"OnStartCommand — flags={flags}, startId={startId}");
            // START_STICKY ensures Android restarts the service if it is killed.
            return StartCommandResult.Sticky;
        }

        /// <summary>Not a bound service; returns null.</summary>
        public override IBinder OnBind(Intent intent) => null;

        /// <summary>Removes pending callbacks and cleans up when the service is destroyed.</summary>
        public override void OnDestroy()
        {
            Log.Info(Tag, "Service destroyed — removing pending callbacks");
            _handler?.RemoveCallbacks(_runnable);
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
            {
                Log.Info(Tag, "Notifications enabled — running password check");
                await PerformCheckAsync(this);
            }
            else
            {
                Log.Info(Tag, "Notifications disabled — skipping check this cycle");
            }

            Log.Info(Tag, $"Next check scheduled in {IntervalMs / MsPerHour} hours");
            _handler.PostDelayed(_runnable, IntervalMs);
        }

        /// <summary>Performs a single password-health check and posts a notification when issues are found.</summary>
        internal static async Task PerformCheckAsync(Context context)
        {
            int userId = await StorageHelper.GetUserId();
            if (userId <= 0)
            {
                Log.Warn(Tag, "No userId found in storage — aborting check (user not logged in?)");
                return;
            }

            Log.Info(Tag, $"Fetching password check for userId={userId}");
            var result = await PasswordCheckService.FetchAsync(userId);
            if (result == null)
            {
                Log.Warn(Tag, "Server returned null result — aborting notification");
                return;
            }

            Log.Info(Tag, $"Check complete — BreachedCount={result.BreachedCount}, OldCount={result.OldCount}, MasterPasswordOld={result.MasterPasswordOld}");
            NotificationHelper.PostCheckResult(context, result);
        }
    }
}
