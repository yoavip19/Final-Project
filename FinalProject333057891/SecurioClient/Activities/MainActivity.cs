using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;

namespace SecurioClient.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    /// <summary>Entry-point activity that validates the stored JWT and routes to VaultActivity or LoginActivity.</summary>
    public class MainActivity : AppCompatActivity
    {
        private const int RequestCodeNotificationPermission = 1001;

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

            if (!string.IsNullOrEmpty(jwt))
            {
                var authService = new AuthService();
                bool tokenValid = await authService.ValidateTokenAsync();

                if (tokenValid)
                {
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
                    // Token is invalid or expired — clear stale credentials before going to login.
                    StorageHelper.ClearAll();
                }
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
                CheckSelfPermission(Android.Manifest.Permission.PostNotifications)
                    != Android.Content.PM.Permission.Granted)
            {
                if (ShouldShowRequestPermissionRationale(Android.Manifest.Permission.PostNotifications))
                {
                    System.Diagnostics.Debug.WriteLine("[NOTIFICATIONS] Showing POST_NOTIFICATIONS permission prompt after prior denial.");
                }

                RequestPermissions(
                    new[] { Android.Manifest.Permission.PostNotifications },
                    RequestCodeNotificationPermission);
            }
            else
            {
                // Permission is already granted (or not needed on this API level).
                // Start the monitor now so it is ready before we navigate to the vault.
                if (_pendingVaultNavigation)
                    StartPasswordMonitor(this);

                NavigateToDest();
            }
        }

        /// <summary>Handles the notification permission result and navigates to the destination screen.</summary>
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestCodeNotificationPermission)
            {
                bool granted = grantResults?.Length > 0 &&
                    grantResults[0] == Android.Content.PM.Permission.Granted;
                System.Diagnostics.Debug.WriteLine(
                    $"[NOTIFICATIONS] POST_NOTIFICATIONS permission {(granted ? "granted" : "denied")}.");

                if (granted && _pendingVaultNavigation)
                {
                    StartPasswordMonitor(this);
                }
                else if (!granted)
                {
                    // User denied notifications — stop the monitor so no server calls are made.
                    StopService(new Android.Content.Intent(this, typeof(PasswordMonitorService)));
                }

                NavigateToDest();
            }
        }

        /// <summary>Navigates to VaultActivity if a valid session exists, otherwise to LoginActivity.</summary>
        private void NavigateToDest()
        {
            Android.Content.Intent intent;
            if (_pendingVaultNavigation)
            {
                intent = new Android.Content.Intent(this, typeof(VaultActivity));
                intent.SetFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.ClearTask);
            }
            else
            {
                intent = new Android.Content.Intent(this, typeof(LoginActivity));
            }

            StartActivity(intent);
            Finish();
        }

        /// <summary>Starts the PasswordMonitorService as a foreground service if it is not already running.</summary>
        public static void StartPasswordMonitor(Android.Content.Context context)
        {
            var serviceIntent = new Android.Content.Intent(context, typeof(PasswordMonitorService));
            context.StartForegroundService(serviceIntent);
        }
    }
}
