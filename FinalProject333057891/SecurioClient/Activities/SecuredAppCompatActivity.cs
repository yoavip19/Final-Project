using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using SecurioClient.Helpers;

namespace SecurioClient.Activities
{
    /// <summary>Base activity for all screens that display sensitive vault data, applying FLAG_SECURE and managing the auto-lock timer and lock screen.</summary>
    public abstract class SecuredAppCompatActivity : AppCompatActivity
    {
        /// <summary>Request code used when starting LockScreenActivity for result.</summary>
        protected const int RequestCodeUnlock = 9002;

        // True while LockScreenActivity is on top to prevent launching a second instance.
        private bool _lockScreenPending;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
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
                    // Reset the timer before unlocking to eliminate any window where
                    // the activity is unlocked but the new countdown has not started yet.
                    AppLockManager.ResetAutoLockTimer();
                    AppLockManager.Unlock();
                }
            }
        }

        /// <summary>Handles the Locked event fired when the inactivity timer expires while this activity is in the foreground.</summary>
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
