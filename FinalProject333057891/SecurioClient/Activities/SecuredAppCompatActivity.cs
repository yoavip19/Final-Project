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

        /// <summary>Subscribes to the AppLockManager.Locked event.</summary>
        protected override void OnStart()
        {
            base.OnStart();
            AppLockManager.Locked += OnAppLocked;
        }

        /// <summary>Unsubscribes from the AppLockManager.Locked event.</summary>
        protected override void OnStop()
        {
            base.OnStop();
            AppLockManager.Locked -= OnAppLocked;
        }

        /// <summary>Checks the lock state and launches the lock screen if the session is locked.</summary>
        protected override void OnResume()
        {
            base.OnResume();
            _lockScreenPending = false;
            if (SessionHelper.IsAuthenticated && AppLockManager.IsLocked)
                LaunchLockScreen();
        }

        /// <summary>Resets the auto-lock inactivity timer on every user interaction.</summary>
        public override void OnUserInteraction()
        {
            base.OnUserInteraction();
            AppLockManager.ResetAutoLockTimer();
        }

        /// <summary>Handles the result from LockScreenActivity and unlocks the session on success.</summary>
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

        /// <summary>Starts LockScreenActivity for a result and sets the pending-lock flag.</summary>
        private void LaunchLockScreen()
        {
            _lockScreenPending = true;
            var intent = new Intent(this, typeof(LockScreenActivity));
            StartActivityForResult(intent, RequestCodeUnlock);
        }
    }
}
