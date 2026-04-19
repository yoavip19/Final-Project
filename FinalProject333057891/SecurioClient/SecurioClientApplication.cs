using Android.App;
using Android.Runtime;
using AndroidX.Work;

namespace SecurioClient
{
    // Custom Application class required to provide WorkManager with a custom
    // Configuration that includes PasswordCheckWorkerFactory. Without this,
    // WorkManager's default factory cannot resolve Xamarin C# worker classes
    // and DoWork() is silently never called.
    //
    // The [Application] attribute tells Xamarin to register this as the Android
    // Application class (sets android:name in the merged manifest automatically).
    [Application]
    public class SecurioClientApplication : Application
    {
        public SecurioClientApplication(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer) { }

        public override void OnCreate()
        {
            base.OnCreate();

            // Initialize WorkManager with a custom factory before any component
            // calls WorkManager.GetInstance(). The auto-initializer (WorkManagerInitializer)
            // is disabled in AndroidManifest.xml so this call does not conflict.
            WorkManager.Initialize(this, new Configuration.Builder()
                .SetWorkerFactory(new PasswordCheckWorkerFactory())
                .Build());
        }
    }
}
