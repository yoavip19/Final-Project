// ============================================================
// TEST WORKER — debugging only. Remove (or stop calling
// TestNotificationWorker.Enqueue) before releasing to production.
// ============================================================
//
// How it works:
//   DoWork() fires an immediate local notification, then calls
//   Enqueue() again with a 10-second InitialDelay.  Because it
//   uses a OneTimeWorkRequest (not Periodic) it is NOT subject to
//   WorkManager's 15-minute periodic floor, so it really fires
//   roughly every 10 seconds while the device is awake/active.
//
// How to use:
//   1. Deploy the app to a device / emulator.
//   2. Open the Vault screen (VaultActivity) — this calls Enqueue().
//   3. Watch for notifications titled "🧪 Worker Test".
//   4. In Android Studio / command line: adb logcat -s TestNotificationWorker PCWorkerFactory
//      to see the logcat output from every DoWork() invocation.
//   5. When done testing, comment out the TestNotificationWorker.Enqueue(this)
//      line in VaultActivity.OnCreate() and redeploy.
//
// What to look for in logcat:
//   PCWorkerFactory : CreateWorker called: received='...', expected='...'
//   PCWorkerFactory : Match — creating TestNotificationWorker instance.
//   TestNotificationWorker: DoWork() run #1.
//   TestNotificationWorker: Re-enqueued (fires in ~10 s).
//
// If you see "No match — delegating to default factory" it means the
// class name comparison failed; check the received vs expected strings
// in the log.
// ============================================================

using Android.Content;
using Android.Util;
using AndroidX.Work;
using SecurioClient.Helpers;
using System;

namespace SecurioClient
{
    public class TestNotificationWorker : Worker
    {
        private const string Tag = "TestNotificationWorker";

        // Unique work name used with EnqueueUniqueWork so only one
        // instance is ever pending at a time.
        private const string UniqueName = "TestNotificationWorker";

        // Notification ID — kept stable so each ping replaces the previous
        // one instead of flooding the notification shade.
        public const int NotificationId = 9999;

        // Static counter so we can see increasing run numbers in the notification.
        // Resets when the process is killed, which is fine for debugging.
        private static int _runCount;

        public TestNotificationWorker(Context context, WorkerParameters workerParams)
            : base(context, workerParams) { }

        public override Result DoWork()
        {
            _runCount++;
            Log.Info(Tag, $"DoWork() run #{_runCount}.");

            NotificationHelper.Show(
                ApplicationContext,
                NotificationId,
                "🧪 Worker Test",
                $"Worker is alive! Run #{_runCount} at {DateTime.Now:HH:mm:ss}");

            // Re-enqueue self so the next ping fires in ~10 seconds.
            Enqueue(ApplicationContext);
            Log.Info(Tag, "Re-enqueued (fires in ~10 s).");

            return Result.InvokeSuccess();
        }

        // -------------------------------------------------------
        // Enqueues a single one-shot run that fires after 10 s.
        // Subsequent runs are enqueued from inside DoWork() above.
        // Safe to call multiple times — ExistingWorkPolicy.Replace
        // ensures only one pending request exists at any time.
        // -------------------------------------------------------
        public static void Enqueue(Context context)
        {
            NotificationHelper.CreateChannel(context);

            var request = new OneTimeWorkRequest.Builder(
                    Java.Lang.Class.FromType(typeof(TestNotificationWorker)))
                .SetInitialDelay(TimeSpan.FromSeconds(10))
                .AddTag(Tag)
                .Build();

            WorkManager.GetInstance(context)
                .EnqueueUniqueWork(UniqueName, ExistingWorkPolicy.Replace, request);

            Log.Info(Tag, "Enqueued (fires in ~10 s).");
        }

        // Cancels any pending test work — call this to stop the ping loop.
        public static void Cancel(Context context)
        {
            WorkManager.GetInstance(context).CancelUniqueWork(UniqueName);
            Log.Info(Tag, "Cancelled.");
        }
    }
}
