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
    /// <summary>
    /// Full-screen lock screen that hides all vault data and requires the user to verify
    /// their identity via biometrics (fingerprint / Face ID) or the device screen-lock PIN
    /// before returning to their session.
    /// <para>
    /// • Back navigation is disabled — the only exit is successful authentication or
    ///   moving the entire task to the background.<br/>
    /// • <c>FLAG_SECURE</c> prevents this screen from being captured in screenshots or
    ///   appearing in the recent-apps switcher.<br/>
    /// • The biometric prompt is triggered automatically on <c>OnResume</c> and can be
    ///   re-triggered manually via the "Unlock" button.
    /// </para>
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", NoHistory = false)]
    public class LockScreenActivity : AppCompatActivity
    {
        // Authenticator flags matching android.hardware.biometrics.BiometricManager.Authenticators
        // (available from Android API 30, which is within our minSdkVersion 33).
        private const int AuthBiometricStrong = 0x000F;
        private const int AuthDeviceCredential = 0x8000;

        private MaterialButton buttonUnlock;

        // Guards against launching a second concurrent biometric prompt.
        private bool _promptShowing;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Prevent screenshots and app-switcher previews of the lock screen.
            Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
            SetContentView(Resource.Layout.activity_lock_screen);

            buttonUnlock = FindViewById<MaterialButton>(Resource.Id.buttonUnlock);
            buttonUnlock.Click += (s, e) => ShowBiometricPrompt();
        }

        protected override void OnResume()
        {
            base.OnResume();
            // Automatically trigger the biometric prompt each time this screen becomes visible.
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
                .SetAllowedAuthenticators(AuthBiometricStrong | AuthDeviceCredential)
                .Build()
                .Authenticate(cancellationSignal, executor, callback);
        }

        public override void OnBackPressed()
        {
            // Security: block back navigation. The only exit is successful authentication.
            // Minimise the entire task rather than finishing this activity.
            MoveTaskToBack(false);
        }

        // ─── Inner helpers ─────────────────────────────────────────────────────────

        /// <summary>Runs <see cref="Java.Util.Concurrent.IExecutor"/> commands on the given Handler's thread.</summary>
        private sealed class HandlerExecutor : Java.Lang.Object, Java.Util.Concurrent.IExecutor
        {
            private readonly Handler _handler;

            public HandlerExecutor(Handler handler) { _handler = handler; }

            // Required by the Xamarin.Android Java binding infrastructure.
            protected HandlerExecutor(IntPtr handle, JniHandleOwnership transfer)
                : base(handle, transfer) { }

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
            protected AuthCallback(IntPtr handle, JniHandleOwnership transfer)
                : base(handle, transfer) { }

            public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
                => _onSuccess?.Invoke();

            public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString)
                => _onError?.Invoke(errorCode, errString?.ToString());

            public override void OnAuthenticationFailed()
            {
                // Biometric scan failed for this attempt (e.g., unrecognised fingerprint).
                // The system prompt handles retries automatically — no action needed here.
            }
        }
    }
}
