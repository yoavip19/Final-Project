using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Util;
using AndroidX.AppCompat.App;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;

namespace SecurioClient.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    /// <summary>Entry-point activity that validates the stored JWT and routes to VaultActivity or LoginActivity.</summary>
    public class MainActivity : AppCompatActivity
    {
        private const string Tag = "MainActivity";

        private const int RequestCodeNotificationPermission = 1001;
        private const int RequestCodeBatteryOptimization    = 1002;

        private const string PrefsName         = "securio_prefs";
        private const string KeyAutostartShown = "autostart_dialog_shown";

        private bool _pendingVaultNavigation = false;

        /// <summary>Initializes the activity, validates the stored JWT, and navigates to the appropriate screen.</summary>
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // Check whether the user already has a stored JWT and validate it with the server.
            // A valid token means the user is already authenticated and can go straight to the Vault.
            string jwt = await StorageHelper.GetJwt();

            if (string.IsNullOrEmpty(jwt))
            {
                Log.Info(Tag, "No stored JWT — session will not be restored");
            }
            else
            {
                Log.Info(Tag, "Stored JWT found — validating with server");
                var authService = new AuthService();
                bool tokenValid = await authService.ValidateTokenAsync();

                if (tokenValid)
                {
                    Log.Info(Tag, "Token valid — restoring session");
                    // Restore the vault key from secure storage so in-memory encryption is ready.
                    string vaultKey = await StorageHelper.GetVaultKey();
                    if (!string.IsNullOrEmpty(vaultKey))
                        SessionHelper.StartSession(vaultKey);

                    // Fetch the user's vault items into the in-memory cache.
                    await AuthService.FetchAndCacheVaultAsync();

                    // Compute password-health warnings for the restored session.
                    // ComputeWarningsSync uses the server-provided IsLeaked flags, so
                    // the count is correct even if the HIBP API is temporarily unreachable.
                    if (!string.IsNullOrEmpty(vaultKey))
                    {
                        SessionHelper.CachedWarnings = WarningsHelper.ComputeWarningsSync(
                            SessionHelper.CachedVault, vaultKey);
                    }

                    _pendingVaultNavigation = true;
                }
                else
                {
                    Log.Warn(Tag, "Token invalid or expired — clearing session and routing to login");
                    // Token is invalid or expired — clear stale credentials before going to login.
                    StorageHelper.ClearAll();
                }
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
                CheckSelfPermission(Android.Manifest.Permission.PostNotifications)
                    != Android.Content.PM.Permission.Granted)
            {
                Log.Info(Tag, "POST_NOTIFICATIONS permission not granted — requesting at runtime");
                RequestPermissions(
                    new[] { Android.Manifest.Permission.PostNotifications },
                    RequestCodeNotificationPermission);
            }
            else
            {
                Log.Info(Tag, "POST_NOTIFICATIONS permission already granted (or not required on this API level)");
                // Permission is already granted (or not needed on this API level).
                if (_pendingVaultNavigation)
                    StartPasswordMonitor(this);

                ShowManufacturerDialogIfNeeded();
            }
        }

        /// <summary>Handles the notification permission result and navigates to the destination screen.</summary>
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestCodeNotificationPermission)
            {
                bool granted = grantResults.Length > 0 &&
                               grantResults[0] == Android.Content.PM.Permission.Granted;
                Log.Info(Tag, $"POST_NOTIFICATIONS permission result: {(granted ? "granted" : "denied")}");

                // Start the monitor regardless of the permission result.
                // The service checks AreNotificationsEnabled on every cycle and skips
                // the server call when notifications are off, so it is safe to keep
                // it running even if the user denied permission for now.
                if (_pendingVaultNavigation)
                    StartPasswordMonitor(this);

                ShowManufacturerDialogIfNeeded();
            }
        }

        /// <summary>Handles the result of the battery-optimisation system dialog and navigates to the destination screen.</summary>
        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == RequestCodeBatteryOptimization)
            {
                Log.Info(Tag, $"Battery optimisation dialog result: {resultCode}");
                NavigateToDest();
            }
        }

        /// <summary>
        /// Requests battery-optimisation exemption if not already granted;
        /// otherwise proceeds directly to manufacturer onboarding.
        /// </summary>
        private void RequestBatteryOptIfNeeded()
        {
            if (!PowerManagerHelper.IsIgnoringBatteryOptimizations(this))
            {
                Log.Info(Tag, "App is NOT battery-optimisation exempt — requesting exemption");
                PowerManagerHelper.RequestIgnoreBatteryOptimizations(this, RequestCodeBatteryOptimization);
            }
            else
            {
                Log.Info(Tag, "App is already battery-optimisation exempt");
                NavigateToDest();
            }
        }

        /// <summary>
        /// Shows a dialog guiding the user to the OEM-specific Autostart / Protected-Apps settings
        /// when the device brand is known to impose additional background restrictions.
        /// The dialog is shown at most once: once the user taps "Open Settings" or "Skip" the
        /// choice is persisted in SharedPreferences and the dialog is never shown again.
        /// Skips straight to navigation on stock Android devices with no known deep-link.
        /// </summary>
        private void ShowManufacturerDialogIfNeeded()
        {
            var intent = PowerManagerHelper.GetManufacturerAutostartIntent();
            if (intent != null && intent.ResolveActivity(PackageManager) != null)
            {
                if (GetSharedPreferences(PrefsName, Android.Content.FileCreationMode.Private)
                        .GetBoolean(KeyAutostartShown, false))
                {
                    Log.Info(Tag, "Autostart dialog already shown previously — skipping");
                    // User has already seen (and acted on) this dialog — don't show it again.
                    RequestBatteryOptIfNeeded();
                    return;
                }

                Log.Info(Tag, $"Showing OEM autostart dialog (brand: {Android.OS.Build.Brand})");
                new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                    .SetTitle("Allow Background Activity")
                    .SetMessage("Your device restricts background apps. To ensure Securio can monitor your passwords reliably, please enable Autostart (or Protected Apps) for Securio in your device settings.")
                    .SetPositiveButton("Open Settings", (s, e) =>
                    {
                        Log.Info(Tag, "User tapped 'Open Settings' on autostart dialog");
                        MarkAutostartDialogShown();
                        try { StartActivity(intent); }
                        catch { Android.Widget.Toast.MakeText(this, "Could not open settings — please enable Autostart for Securio manually.", Android.Widget.ToastLength.Long)?.Show(); }
                        RequestBatteryOptIfNeeded();
                    })
                    .SetNegativeButton("Skip", (s, e) =>
                    {
                        Log.Info(Tag, "User tapped 'Skip' on autostart dialog");
                        MarkAutostartDialogShown();
                        RequestBatteryOptIfNeeded();
                    })
                    .SetCancelable(false)
                    .Show();
            }
            else
            {
                Log.Info(Tag, "No OEM autostart intent available (stock Android or unrecognised brand) — skipping dialog");
                RequestBatteryOptIfNeeded();
            }
        }

        /// <summary>Persists a flag so the Autostart onboarding dialog is not shown again on future launches.</summary>
        private void MarkAutostartDialogShown()
        {
            GetSharedPreferences(PrefsName, Android.Content.FileCreationMode.Private)
                .Edit()
                .PutBoolean(KeyAutostartShown, true)
                .Apply();
        }

        /// <summary>Navigates to VaultActivity if a valid session exists, otherwise to LoginActivity.</summary>
        private void NavigateToDest()
        {
            Android.Content.Intent intent;
            if (_pendingVaultNavigation)
            {
                Log.Info(Tag, "Navigating to VaultActivity");
                intent = new Android.Content.Intent(this, typeof(VaultActivity));
                intent.SetFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.ClearTask);
            }
            else
            {
                Log.Info(Tag, "Navigating to LoginActivity");
                intent = new Android.Content.Intent(this, typeof(LoginActivity));
            }

            StartActivity(intent);
            Finish();
        }

        /// <summary>Starts the PasswordMonitorService as a foreground service if it is not already running.</summary>
        public static void StartPasswordMonitor(Android.Content.Context context)
        {
            Log.Info(Tag, "Starting PasswordMonitorService as foreground service");
            var serviceIntent = new Android.Content.Intent(context, typeof(PasswordMonitorService));
            context.StartForegroundService(serviceIntent);
        }
    }
}
