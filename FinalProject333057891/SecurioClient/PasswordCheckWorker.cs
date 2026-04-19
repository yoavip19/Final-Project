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

        // How often the check runs in production.
        // For testing: change to 1 minute (PeriodicWorkRequest minimum is 15 min, so
        // the actual floor is 15 min — but the intent is documented here).
        //public const int IntervalHours = 24 // Uncomment for production
        public const int IntervalMinutes = 1;

        public PasswordCheckWorker(Context context, WorkerParameters workerParams)
            : base(context, workerParams)
        {
        }

        public override Result DoWork()
        {
            try
            {
                // Run the async poll synchronously inside the Worker thread.
                var task = RunCheckAsync();
                task.Wait();
                return Result.InvokeSuccess();
            }
            catch (Exception ex)
            {
                Log.Error(Tag, $"Password check failed: {ex.Message}");
                return Result.InvokeRetry();
            }
        }

        private async Task RunCheckAsync()
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
                    ApplicationContext,
                    NotificationHelper.NotificationIdBreach,
                    "⚠️ Breached Passwords Detected",
                    $"{username}, {data.BreachedCount} of your passwords appeared in known data breaches. Change them now.");
            }

            if (data.OldCount > 0)
            {
                NotificationHelper.Show(
                    ApplicationContext,
                    NotificationHelper.NotificationIdOld,
                    "🕐 Old Passwords Detected",
                    $"{username}, {data.OldCount} of your passwords haven't been updated in over 90 days.");
            }

            if (data.MasterPasswordOld)
            {
                NotificationHelper.Show(
                    ApplicationContext,
                    NotificationHelper.NotificationIdMaster,
                    "🔑 Master Password Needs Update",
                    $"{username}, your master password hasn't been changed in over 90 days. Consider updating it.");
            }
        }

        // Enqueues the periodic work request with WorkManager.
        // Call once at app startup; WorkManager de-duplicates by unique name.
        public static void Enqueue(Context context)
        {
            // Create the notification channel early so the first notification works.
            NotificationHelper.CreateChannel(context);

            var request = PeriodicWorkRequest.Builder
                //.From<PasswordCheckWorker>(TimeSpan.FromHours(IntervalHours)) // Uncomment for production
                .From<PasswordCheckWorker>(TimeSpan.FromMinutes(IntervalMinutes))
                .AddTag(Tag)
                .Build();

            WorkManager.GetInstance(context)
                .EnqueueUniquePeriodicWork(
                    Tag,
                    ExistingPeriodicWorkPolicy.Keep,
                    request);
        }
    }
}
