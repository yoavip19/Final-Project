using Android.Content;
using AndroidX.Work;

namespace SecurioClient
{
    // A custom WorkerFactory that allows WorkManager to instantiate Xamarin.Android
    // worker classes. The default WorkerFactory uses Java class-loading which cannot
    // resolve C# types — this factory bridges that gap.
    public class PasswordCheckWorkerFactory : WorkerFactory
    {
        public override ListenableWorker CreateWorker(
            Context appContext,
            string workerClassName,
            WorkerParameters workerParameters)
        {
            // Match by the Java class name that Xamarin generates for this C# type.
            if (workerClassName == Java.Lang.Class.FromType(typeof(PasswordCheckWorker)).CanonicalName)
                return new PasswordCheckWorker(appContext, workerParameters);

            // Return null to let the default factory handle any other worker types.
            return null;
        }
    }
}
