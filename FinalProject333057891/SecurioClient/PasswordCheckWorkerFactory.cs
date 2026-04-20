using Android.Content;
using Android.Util;
using AndroidX.Work;

namespace SecurioClient
{
    // A custom WorkerFactory that allows WorkManager to instantiate Xamarin.Android
    // worker classes.  The default WorkerFactory uses Java class-loading which cannot
    // resolve C# types — this factory bridges that gap.
    public class PasswordCheckWorkerFactory : WorkerFactory
    {
        private const string Tag = "PCWorkerFactory";

        public override ListenableWorker CreateWorker(
            Context appContext,
            string workerClassName,
            WorkerParameters workerParameters)
        {
            // Use .Name (maps to Java getName()) which is what WorkManager stores
            // when the work request is created.
            Log.Info(Tag, $"CreateWorker called: received='{workerClassName}'");

            var testWorkerName = Java.Lang.Class.FromType(typeof(TestNotificationWorker)).Name;
            if (workerClassName == testWorkerName)
            {
                Log.Info(Tag, "Match — creating TestNotificationWorker instance.");
                return new TestNotificationWorker(appContext, workerParameters);
            }

            Log.Warn(Tag, $"No match for '{workerClassName}' — delegating to default factory.");
            return null;
        }
    }
}
