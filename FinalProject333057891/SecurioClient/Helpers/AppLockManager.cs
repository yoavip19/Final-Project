using Android.OS;
using System;

namespace SecurioClient.Helpers
{
    /// <summary>Manages the app-lock state and the 2-minute inactivity auto-lock timer that fires after AutoLockDelayMs of silence and notifies the foreground activity via the Locked event.</summary>
    public static class AppLockManager
    {
        private const long AutoLockDelayMs = 2 * 60 * 1000L; // 2 minutes

        private static bool _isLocked;

        // A single Handler + Runnable pair ensures RemoveCallbacks always targets
        // the correct pending callback and avoids orphaned runnable instances.
        private static readonly Handler _handler = new Handler(Looper.MainLooper);
        private static readonly Java.Lang.Runnable _lockRunnable = new Java.Lang.Runnable(Lock);

        /// <summary>Gets a value indicating whether the app is currently locked.</summary>
        public static bool IsLocked => _isLocked;

        /// <summary>
        /// Raised on the main thread when the app transitions to the locked state.
        /// Protected activities subscribe to this event while in the foreground so
        /// they can display the lock screen immediately when the timer fires.
        /// </summary>
        public static event Action Locked;

        /// <summary>Locks the app, requiring biometric or device-credential authentication to resume.</summary>
        public static void Lock()
        {
            _isLocked = true;
            CancelAutoLockTimer();
            Locked?.Invoke();
        }

        /// <summary>Unlocks the app after successful authentication.</summary>
        public static void Unlock()
        {
            _isLocked = false;
        }

        /// <summary>Resets the 2-minute inactivity timer; call this from OnUserInteraction in every protected activity.</summary>
        public static void ResetAutoLockTimer()
        {
            if (!SessionHelper.IsAuthenticated)
                return;

            _handler.RemoveCallbacks(_lockRunnable);
            _handler.PostDelayed(_lockRunnable, AutoLockDelayMs);
        }

        /// <summary>Cancels any pending auto-lock countdown without changing the locked state.</summary>
        public static void CancelAutoLockTimer()
        {
            _handler.RemoveCallbacks(_lockRunnable);
        }
    }
}
