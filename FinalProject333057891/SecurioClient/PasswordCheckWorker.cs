using Android.Content;
using Android.Util;
using AndroidX.Work;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using System;
using System.Threading.Tasks;

namespace SecurioClient
{
    // A periodic background worker that polls the server for password-health issues
    // and fires local notifications when problems are found.
    // Scheduled via AndroidX WorkManager so it runs even when the app is not in the foreground.
    public class PasswordCheckWorker : Worker
    {
        private const string Tag = "PasswordCheckWorker";
        private const string DefaultUsername = "User";

        // WorkManager enforces a minimum periodic interval of 15 minutes.
        // Setting a value below 15 is silently clamped, so we use the true floor.
        // For production, change to 24 hours (1440 minutes).
        public const int IntervalMinutes = 15; // 15 for testing, 1440 for production

        public PasswordCheckWorker(Context context, WorkerParameters workerParams)
            : base(context, workerParams)
        {
        }

        public override Result DoWork()
        {
            Log.Info(Tag, "DoWork() entered.");
            try
            {
                // Run the async poll synchronously inside the Worker thread.
                var task = RunCheckAsync(ApplicationContext);
                task.Wait();
                Log.Info(Tag, "DoWork() completed successfully.");
                return Result.InvokeSuccess();
            }
            catch (Exception ex)
            {
                Log.Error(Tag, $"Password check failed: {ex.Message}");
                return Result.InvokeRetry();
            }
        }

        // Public static so it can be reused by PasswordMonitorService without duplicating logic.
        public static async Task RunCheckAsync(Context context)
        {
            // Retrieve the persisted user ID from secure storage (survives logout).
            int userId = await StorageHelper.GetUserId();
            if (userId <= 0)
            {
                Log.Info(Tag, "No stored user ID — skipping password check.");
                return;
            }

            var service = new PasswordCheckService();
            var (success, message, data) = await service.CheckAsync(userId);

            if (!success || data == null)
            {
                Log.Warn(Tag, $"Server check failed: {message}");
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
        }

        // Enqueues the periodic work request with WorkManager.
        // Uses Replace policy to ensure configuration changes are picked up
        // and any stale work from before the custom WorkerFactory fix is cleared.
        public static void Enqueue(Context context)
        {
            // Create the notification channel early so the first notification works.
            NotificationHelper.CreateChannel(context);

            // Require network connectivity — the worker calls the server.
            var constraints = new Constraints.Builder()
                .SetRequiredNetworkType(NetworkType.Connected)
                .Build();

            var request = PeriodicWorkRequest.Builder
                .From<PasswordCheckWorker>(TimeSpan.FromMinutes(IntervalMinutes))
                .SetConstraints(constraints)
                .AddTag(Tag)
                .Build();

            // Replace ensures stale requests (from before the factory fix) are cleared.
            WorkManager.GetInstance(context)
                .EnqueueUniquePeriodicWork(
                    Tag,
                    ExistingPeriodicWorkPolicy.Replace,
                    request);

            Log.Info(Tag, $"Periodic work enqueued (interval={IntervalMinutes}min, policy=Replace).");
        }
    }
}
