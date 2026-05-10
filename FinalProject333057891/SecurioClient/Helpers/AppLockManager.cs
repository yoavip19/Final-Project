using Android.OS;
using System;

namespace SecurioClient.Helpers
{
    /// <summary>
    /// Manages the app-lock state and the 2-minute inactivity auto-lock timer.
    /// The timer is reset on every user interaction in a protected activity and fires
    /// after <see cref="AutoLockDelayMs"/> of silence, locking the session.
    /// The <see cref="Locked"/> event notifies the foreground activity immediately so
    /// the lock screen can appear even while the app is in the foreground.
    /// </summary>
    public static class AppLockManager
    {
        private const long AutoLockDelayMs = 2 * 60 * 1000L; // 2 minutes

        private static bool _isLocked;
        private static Handler _handler;
        private static Java.Lang.Runnable _lockRunnable;

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

        /// <summary>
        /// Resets the 2-minute inactivity timer. Call this from
        /// <c>OnUserInteraction</c> in every protected activity.
        /// Has no effect when no session is active.
        /// </summary>
        public static void ResetAutoLockTimer()
        {
            if (!SessionHelper.IsAuthenticated)
                return;

            CancelAutoLockTimer();

            if (_handler == null)
                _handler = new Handler(Looper.MainLooper);

            _lockRunnable = new Java.Lang.Runnable(Lock);
            _handler.PostDelayed(_lockRunnable, AutoLockDelayMs);
        }

        /// <summary>Cancels any pending auto-lock countdown without changing the locked state.</summary>
        public static void CancelAutoLockTimer()
        {
            if (_handler != null && _lockRunnable != null)
            {
                _handler.RemoveCallbacks(_lockRunnable);
                _lockRunnable = null;
            }
        }
    }
}
