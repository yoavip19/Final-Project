using Android.App;
using Android.Runtime;
using Android.Util;
using AndroidX.Work;
using System;

namespace SecurioClient
{
    // Custom Application class that provides WorkManager with a Configuration
    // containing our PasswordCheckWorkerFactory.
    //
    // Implements Configuration.IProvider so the WorkManager auto-initializer
    // (via App Startup ContentProvider) uses our factory even though it runs
    // BEFORE Application.OnCreate().  This is the recommended AndroidX Work
    // approach and does not depend on tools:node="remove" in the manifest.
    //
    // A manual Initialize() call in OnCreate() acts as a belt-and-suspenders
    // fallback for the case where the auto-initializer was successfully
    // disabled via the manifest.
    [Application]
    public class SecurioClientApplication : Application, Configuration.IProvider
    {
        private const string Tag = "SecurioApp";

        public SecurioClientApplication(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer) { }

        // Called by WorkManager's auto-initializer when it detects that
        // the Application implements Configuration.IProvider.
        public Configuration WorkManagerConfiguration =>
            new Configuration.Builder()
                .SetWorkerFactory(new PasswordCheckWorkerFactory())
                .SetMinimumLoggingLevel((int)LogPriority.Info)
                .Build();

        public override void OnCreate()
        {
            base.OnCreate();

            // If the auto-initializer was disabled via tools:node="remove",
            // WorkManager is not yet initialised — do it now.
            // If auto-init already ran (using our IProvider config above),
            // Initialize() throws IllegalStateException — catch and ignore.
            try
            {
                WorkManager.Initialize(this, WorkManagerConfiguration);
                Log.Info(Tag, "WorkManager initialised manually (auto-init was disabled).");
            }
            catch (Java.Lang.IllegalStateException)
            {
                Log.Info(Tag, "WorkManager already initialised by auto-init with IProvider config.");
            }
        }
    }
}
