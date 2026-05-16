using Android.App;
using Android.Hardware.Biometrics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Google.Android.Material.Button;
using SecurioClient.Helpers;
using System;

namespace SecurioClient.Activities
{
    /// <summary>Full-screen lock screen that hides all vault data and requires biometric or device-credential authentication before returning to the session.</summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", NoHistory = false)]
    public class LockScreenActivity : AppCompatActivity
    {
        // Authenticator type flags from android.hardware.biometrics.BiometricManager.Authenticators (API 30+).
        // Values are stable platform constants; using them directly ensures compatibility
        // across Xamarin.Android binding versions.
        private const int BiometricStrong = 15;    // BiometricManager.Authenticators.BIOMETRIC_STRONG
        private const int DeviceCredential = 32768; // BiometricManager.Authenticators.DEVICE_CREDENTIAL

        private MaterialButton buttonUnlock;
        private MaterialButton buttonLogout;

        // Guards against launching a second concurrent biometric prompt.
        private bool _promptShowing;

        /// <summary>Inflates the layout, applies FLAG_SECURE, and wires up the biometric prompt.</summary>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
            SetContentView(Resource.Layout.activity_lock_screen);

            buttonUnlock = FindViewById<MaterialButton>(Resource.Id.buttonUnlock);
            buttonLogout = FindViewById<MaterialButton>(Resource.Id.buttonLockLogout);
            buttonUnlock.Click += (s, e) => ShowBiometricPrompt();
            buttonLogout.Click += (s, e) => LogoutHelper.ShowLogoutConfirmation(this);
        }

        /// <summary>Triggers the biometric prompt automatically when the screen is shown.</summary>
        protected override void OnResume()
        {
            base.OnResume();
            ShowBiometricPrompt();
        }

        /// <summary>Displays the system biometric / device-credential prompt.</summary>
        private void ShowBiometricPrompt()
        {
            if (_promptShowing)
                return;

            _promptShowing = true;
            var cancellationSignal = new CancellationSignal();
            var executor = new HandlerExecutor(new Handler(Looper.MainLooper));

            var callback = new AuthCallback(
                onSuccess: () =>
                {
                    _promptShowing = false;
                    SetResult(Result.Ok);
                    Finish();
                },
                onError: (code, msg) =>
                {
                    // Authentication cancelled or hardware error — keep the lock screen
                    // visible so the user can tap "Unlock" to retry.
                    _promptShowing = false;
                }
            );

            new BiometricPrompt.Builder(this)
                .SetTitle(GetString(Resource.String.lock_screen_biometric_title))
                .SetSubtitle(GetString(Resource.String.lock_screen_biometric_subtitle))
                // BIOMETRIC_STRONG | DEVICE_CREDENTIAL: tries biometrics first and falls back
                // to the device PIN / pattern / password if biometrics are unavailable.
                // SetNegativeButton must NOT be called when DeviceCredential is included.
                .SetAllowedAuthenticators(BiometricStrong | DeviceCredential)
                .Build()
                .Authenticate(cancellationSignal, executor, callback);
        }

        /// <summary>Suppresses the back button to prevent bypassing the lock screen.</summary>
        public override void OnBackPressed()
        {
            // Security: block back navigation. The only exit is successful authentication.
            // Minimise the entire task rather than finishing this activity.
            MoveTaskToBack(true);
        }

        /// <summary>Runs IExecutor commands on the given Handler's thread.</summary>
        private sealed class HandlerExecutor : Java.Lang.Object, Java.Util.Concurrent.IExecutor
        {
            private readonly Handler _handler;

            public HandlerExecutor(Handler handler) { _handler = handler; }

            // Required by the Xamarin.Android Java binding infrastructure.
            /// <summary>JNI constructor for Android runtime use.</summary>
            protected HandlerExecutor(IntPtr handle, JniHandleOwnership transfer)
                : base(handle, transfer) { }

            /// <summary>Posts the given runnable to the handler's thread.</summary>
            public void Execute(Java.Lang.IRunnable command) => _handler.Post(command);
        }

        /// <summary>Biometric authentication callback that delegates results to plain C# actions.</summary>
        private sealed class AuthCallback : BiometricPrompt.AuthenticationCallback
        {
            private readonly Action _onSuccess;
            private readonly Action<int, string> _onError;

            public AuthCallback(Action onSuccess, Action<int, string> onError)
            {
                _onSuccess = onSuccess;
                _onError = onError;
            }

            // Required by the Xamarin.Android Java binding infrastructure.
            /// <summary>JNI constructor for Android runtime use.</summary>
            protected AuthCallback(IntPtr handle, JniHandleOwnership transfer)
                : base(handle, transfer) { }

            /// <summary>Handles successful biometric authentication.</summary>
            public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
                => _onSuccess?.Invoke();

            /// <summary>Handles a non-recoverable biometric authentication error.</summary>
            public override void OnAuthenticationError(BiometricErrorCode errorCode, Java.Lang.ICharSequence errString)
                => _onError?.Invoke((int)errorCode, errString?.ToString());

            /// <summary>Called when biometric authentication fails but can be retried.</summary>
            public override void OnAuthenticationFailed()
            {
                // Biometric scan failed for this attempt (e.g., unrecognised fingerprint).
                // The system prompt handles retries automatically — no action needed here.
            }
        }
    }
}
