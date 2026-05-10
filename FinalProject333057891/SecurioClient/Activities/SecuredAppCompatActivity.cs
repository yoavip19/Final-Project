using Android.Content;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using SecurioClient.Helpers;

namespace SecurioClient.Activities
{
    /// <summary>
    /// Base activity for all screens that display sensitive vault data.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Applies <c>FLAG_SECURE</c> to prevent the screen from appearing in the
    ///         recent-apps thumbnail and to block screenshots.</item>
    ///   <item>Checks the lock state on every resume and immediately launches
    ///         <see cref="LockScreenActivity"/> if the session is locked.</item>
    ///   <item>Subscribes to the <see cref="AppLockManager.Locked"/> event while in the
    ///         foreground so the lock screen appears even during active use (timer fires
    ///         after 2 min of inactivity).</item>
    ///   <item>Resets the inactivity timer on every user interaction.</item>
    /// </list>
    /// </para>
    /// </summary>
    public abstract class SecuredAppCompatActivity : AppCompatActivity
    {
        /// <summary>Request code used when starting <see cref="LockScreenActivity"/> for result.</summary>
        protected const int RequestCodeUnlock = 9002;

        // True while LockScreenActivity is on top to prevent launching a second instance.
        private bool _lockScreenPending;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Prevent sensitive data from appearing in the recent-apps thumbnail and block screenshots.
            Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
        }

        protected override void OnStart()
        {
            base.OnStart();
            AppLockManager.Locked += OnAppLocked;
        }

        protected override void OnStop()
        {
            base.OnStop();
            AppLockManager.Locked -= OnAppLocked;
        }

        protected override void OnResume()
        {
            base.OnResume();
            _lockScreenPending = false;
            if (SessionHelper.IsAuthenticated && AppLockManager.IsLocked)
                LaunchLockScreen();
        }

        public override void OnUserInteraction()
        {
            base.OnUserInteraction();
            AppLockManager.ResetAutoLockTimer();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == RequestCodeUnlock)
            {
                _lockScreenPending = false;
                if (resultCode == Result.Ok)
                {
                    AppLockManager.Unlock();
                    AppLockManager.ResetAutoLockTimer();
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="AppLockManager.Locked"/> event fired when the inactivity
        /// timer expires while this activity is in the foreground.
        /// </summary>
        private void OnAppLocked()
        {
            RunOnUiThread(() =>
            {
                if (!_lockScreenPending)
                    LaunchLockScreen();
            });
        }

        private void LaunchLockScreen()
        {
            _lockScreenPending = true;
            var intent = new Intent(this, typeof(LockScreenActivity));
            StartActivityForResult(intent, RequestCodeUnlock);
        }
    }
}
