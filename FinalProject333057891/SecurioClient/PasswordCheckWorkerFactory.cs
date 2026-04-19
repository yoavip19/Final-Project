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
            var expectedName = Java.Lang.Class.FromType(typeof(PasswordCheckWorker)).Name;
            Log.Info(Tag, $"CreateWorker called: received='{workerClassName}', expected='{expectedName}'");

            if (workerClassName == expectedName)
            {
                Log.Info(Tag, "Match — creating PasswordCheckWorker instance.");
                return new PasswordCheckWorker(appContext, workerParameters);
            }

            Log.Warn(Tag, "No match — delegating to default factory.");
            return null;
        }
    }
}
