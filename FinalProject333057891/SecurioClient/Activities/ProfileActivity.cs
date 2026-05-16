using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Button;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using SecurioModels.DataTransferObjects;
using System;
using System.Threading.Tasks;

namespace SecurioClient.Activities
{
    /// <summary>Activity that displays the user's profile information with options to edit, log out, or delete the account.</summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class ProfileActivity : SecuredAppCompatActivity
    {
        private const int RequestCodeNotificationPermission = 1003;

        private TextView textViewProfileUsername;
        private TextView textViewProfileEmail;
        private TextView textViewProfileLastLogin;
        private TextView textViewProfileLastPasswordChange;
        private TextView textViewProfileCreatedAt;
        private TextView textViewProfilePasswordCount;
        private MaterialButton buttonProfileEdit;
        private MaterialButton buttonProfileNotifications;
        private MaterialButton buttonProfileLogout;
        private MaterialButton buttonProfileDelete;
        private ProgressBar progressBarProfile;

        /// <summary>Initializes the activity, inflates the layout, and loads the user profile.</summary>
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_profile);

            InitializeViews();
            SetupBottomNavFragment(savedInstanceState);
            SetupEventHandlers();

            await RestoreSessionStateIfNeededAsync();
            await LoadProfileAsync();
        }

        /// <summary>Refreshes UI state when returning from external screens such as system notification settings.</summary>
        protected override void OnResume()
        {
            base.OnResume();
            EnsurePasswordMonitorNotification();
            UpdateNotificationButton();
        }

        /// <summary>Finds and assigns all view references from the layout.</summary>
        private void InitializeViews()
        {
            textViewProfileUsername = FindViewById<TextView>(Resource.Id.textViewProfileUsername);
            textViewProfileEmail = FindViewById<TextView>(Resource.Id.textViewProfileEmail);
            textViewProfileLastLogin = FindViewById<TextView>(Resource.Id.textViewProfileLastLogin);
            textViewProfileLastPasswordChange = FindViewById<TextView>(Resource.Id.textViewProfileLastPasswordChange);
            textViewProfileCreatedAt = FindViewById<TextView>(Resource.Id.textViewProfileCreatedAt);
            textViewProfilePasswordCount = FindViewById<TextView>(Resource.Id.textViewProfilePasswordCount);
            buttonProfileEdit = FindViewById<MaterialButton>(Resource.Id.buttonProfileEdit);
            buttonProfileNotifications = FindViewById<MaterialButton>(Resource.Id.buttonProfileNotifications);
            buttonProfileLogout = FindViewById<MaterialButton>(Resource.Id.buttonProfileLogout);
            buttonProfileDelete = FindViewById<MaterialButton>(Resource.Id.buttonProfileDelete);
            progressBarProfile = FindViewById<ProgressBar>(Resource.Id.progressBarProfile);

            UpdateNotificationButton();
        }

        /// <summary>Adds the BottomNavFragment on first creation to avoid duplicate fragments on configuration change.</summary>
        private void SetupBottomNavFragment(Bundle savedInstanceState)
        {
            var fragment = SupportFragmentManager.FindFragmentById(Resource.Id.frameBottomNav) as BottomNavFragment;

            if (fragment == null)
            {
                fragment = BottomNavFragment.NewInstance("profile");

                SupportFragmentManager
                    .BeginTransaction()
                    .Replace(Resource.Id.frameBottomNav, fragment)
                    .Commit();
            }

            fragment.TabSelected -= OnBottomNavTabSelected;
            fragment.TabSelected += OnBottomNavTabSelected;
        }

        /// <summary>Wires up click handlers for the edit, notifications, logout, and delete account buttons.</summary>
        private void SetupEventHandlers()
        {
            buttonProfileEdit.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(EditAccountActivity));
                StartActivityForResult(intent, EditAccountActivity.RequestCodeEditAccount);
            };

            buttonProfileNotifications.Click += (sender, e) => ManageNotifications();
            buttonProfileLogout.Click += (sender, e) => ConfirmLogout();
            buttonProfileDelete.Click += (sender, e) => ConfirmDeleteAccount();
        }

        /// <summary>Delegates bottom navigation tab selection to BottomNavHelper.</summary>
        private void OnBottomNavTabSelected(object sender, string tab)
            => BottomNavHelper.Navigate(this, tab, "profile");

        /// <summary>Reloads the profile after a successful account edit.</summary>
        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == EditAccountActivity.RequestCodeEditAccount
                && resultCode == Result.Ok
                && data?.GetBooleanExtra(EditAccountActivity.ResultUpdated, false) == true)
            {
                // Invalidate the session cache so the next load fetches fresh data from the server.
                SessionHelper.InvalidateProfile();
                await LoadProfileAsync();
            }
        }

        /// <summary>Displays the user profile using the in-memory session cache when available, or fetches from the server on cache miss.</summary>
        private async Task LoadProfileAsync()
        {
            var cached = SessionHelper.CachedProfile;

            if (cached != null)
            {
                // Serve from the in-memory cache — no server call needed.
                // DisplayProfile always derives PasswordCount from CachedVault.Count.
                DisplayProfile(cached);
                return;
            }

            // Cache miss — fetch from the server.
            progressBarProfile.Visibility = ViewStates.Visible;

            try
            {
                var profileService = new ProfileService();
                var result = await profileService.GetProfileAsync();

                if (result.Success && result.Data != null)
                {
                    SessionHelper.CachedProfile = result.Data;
                    DisplayProfile(result.Data);
                }
                else
                {
                    // Fall back to cached profile data
                    var diskCached = await StorageHelper.GetCachedProfileAsync();
                    if (diskCached != null)
                    {
                        DisplayProfile(diskCached);
                        Toast.MakeText(this, Resource.String.profile_load_error, ToastLength.Short).Show();
                    }
                    else
                    {
                        Toast.MakeText(this, result.Message ?? "Unable to load profile.", ToastLength.Long).Show();
                    }
                }
            }
            catch (Exception)
            {
                // Fall back to cached profile data
                var diskCached = await StorageHelper.GetCachedProfileAsync();
                if (diskCached != null)
                {
                    DisplayProfile(diskCached);
                    Toast.MakeText(this, Resource.String.profile_load_error, ToastLength.Short).Show();
                }
            }
            finally
            {
                progressBarProfile.Visibility = ViewStates.Gone;
            }
        }

        /// <summary>Restores the in-memory session key and vault cache after activity/process recreation when secure storage still has credentials.</summary>
        private async Task RestoreSessionStateIfNeededAsync()
        {
            if (SessionHelper.IsAuthenticated)
                return;

            string jwt = await StorageHelper.GetJwtAsync();
            string vaultKey = await StorageHelper.GetVaultKeyAsync();
            if (string.IsNullOrEmpty(jwt) || string.IsNullOrEmpty(vaultKey))
                return;

            SessionHelper.StartSession(vaultKey);
            await AuthService.FetchAndCacheVaultAsync();
        }

        /// <summary>Populates the UI text views with the given user profile data.</summary>
        private void DisplayProfile(User profile)
        {
            textViewProfileUsername.Text = profile.Username ?? "—";
            textViewProfileEmail.Text = profile.Email ?? "—";
            textViewProfileLastLogin.Text = FormatDate(profile.LastLogin);
            textViewProfileLastPasswordChange.Text = FormatDate(profile.LastPasswordUpdate);
            textViewProfileCreatedAt.Text = FormatDate(profile.CreatedAt);
            textViewProfilePasswordCount.Text = GetDisplayedPasswordCount(profile).ToString();
        }

        /// <summary>Uses the live session vault count when available, otherwise falls back to the profile payload/cache.</summary>
        private int GetDisplayedPasswordCount(User profile)
        {
            if (CanTrustLiveVaultCount(profile))
            {
                return SessionHelper.CachedVault.Count;
            }

            return Math.Max(0, profile?.PasswordCount ?? 0);
        }

        /// <summary>Determines whether the live in-memory vault count is trustworthy for the current session.</summary>
        private bool CanTrustLiveVaultCount(User profile)
        {
            if (!SessionHelper.IsAuthenticated)
                return false;

            // Trust the live cache when it has entries, or when both the live cache and
            // the profile payload indicate an empty vault (0 is a legitimate value).
            return SessionHelper.CachedVault.Count > 0 || (profile?.PasswordCount ?? 0) == 0;
        }

        /// <summary>Formats a DateTime as a locale-friendly string, returning "—" for the minimum value.</summary>
        private string FormatDate(DateTime date)
        {
            if (date == DateTime.MinValue)
                return "—";

            return (date.Kind == DateTimeKind.Local ? date : DateTime.SpecifyKind(date, DateTimeKind.Utc))
                .ToLocalTime()
                .ToString("MMM dd, yyyy h:mm tt");
        }

        /// <summary>Updates the notification button text to reflect the app's current system notification state.</summary>
        private void UpdateNotificationButton()
        {
            bool enabled = NotificationHelper.AreNotificationsEnabled(this);
            buttonProfileNotifications.Text = enabled
                ? GetString(Resource.String.profile_button_notifications_on)
                : GetString(Resource.String.profile_button_notifications_off);
        }

        /// <summary>Ensures the foreground monitor service (and its persistent notification) is running whenever notifications are enabled.</summary>
        private void EnsurePasswordMonitorNotification()
        {
            if (NotificationHelper.AreNotificationsEnabled(this))
                PasswordMonitorService.StartMonitoring(this);
        }

        /// <summary>Requests notification permission when needed and then opens the app's notification settings screen.</summary>
        private void ManageNotifications()
        {
            if (ShouldRequestNotificationPermission())
            {
                RequestPermissions(
                    new[] { Android.Manifest.Permission.PostNotifications },
                    RequestCodeNotificationPermission);
                return;
            }

            OpenNotificationSettings();
        }

        /// <summary>Returns whether Android 13+ notification runtime permission still needs to be requested.</summary>
        private bool ShouldRequestNotificationPermission()
        {
            // Android 13 (Tiramisu) introduced POST_NOTIFICATIONS as a runtime permission.
            return Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
                   CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted;
        }

        /// <summary>Handles runtime permission results for the notification button flow.</summary>
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestCodeNotificationPermission)
            {
                OpenNotificationSettings();
                UpdateNotificationButton();
            }
        }

        /// <summary>Opens this app's notification settings screen so the user can enable/disable notifications at OS level.</summary>
        private void OpenNotificationSettings()
        {
            try
            {
                var intent = new Intent(Settings.ActionAppNotificationSettings);
                intent.PutExtra(Settings.ExtraAppPackage, PackageName);
                StartActivity(intent);
            }
            catch (ActivityNotFoundException)
            {
                var fallbackIntent = new Intent(Settings.ActionApplicationDetailsSettings);
                fallbackIntent.SetData(Android.Net.Uri.Parse("package:" + PackageName));
                StartActivity(fallbackIntent);
            }
        }

        /// <summary>Shows a confirmation dialog before logging out.</summary>
        private void ConfirmLogout()
            => LogoutHelper.ShowLogoutConfirmation(this);

        /// <summary>Shows a confirmation dialog before deleting the account.</summary>
        private void ConfirmDeleteAccount()
        {
            new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.profile_delete_confirm_title)
                .SetMessage(Resource.String.profile_delete_confirm_message)
                .SetPositiveButton(Resource.String.profile_delete_confirm_yes, async (s, e) =>
                {
                    await DeleteAccountAsync();
                })
                .SetNegativeButton(Resource.String.profile_delete_confirm_no, (s, e) => { })
                .Show();
        }

        /// <summary>Calls ProfileService to delete the account and navigates to LoginActivity on success.</summary>
        private async Task DeleteAccountAsync()
        {
            progressBarProfile.Visibility = ViewStates.Visible;
            buttonProfileDelete.Enabled = false;

            try
            {
                var profileService = new ProfileService();
                var result = await profileService.DeleteAccountAsync();

                if (result.Success)
                {
                    Toast.MakeText(this, Resource.String.profile_deleted_toast, ToastLength.Short).Show();

                    AppLockManager.CancelAutoLockTimer();
                    AppLockManager.Unlock();
                    SessionHelper.EndSession();
                    StorageHelper.ClearAll();

                    var intent = new Intent(this, typeof(LoginActivity));
                    intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
                    StartActivity(intent);
                    Finish();
                }
                else
                {
                    Toast.MakeText(this, Resource.String.profile_delete_error, ToastLength.Long).Show();
                }
            }
            catch (Exception)
            {
                Toast.MakeText(this, Resource.String.profile_delete_error, ToastLength.Long).Show();
            }
            finally
            {
                progressBarProfile.Visibility = ViewStates.Gone;
                buttonProfileDelete.Enabled = true;
            }
        }
    }
}
