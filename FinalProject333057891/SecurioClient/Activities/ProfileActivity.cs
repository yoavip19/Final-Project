using Android.App;
using Android.Content;
using Android.OS;
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
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    /// <summary>Activity that displays the user's profile information with options to edit, log out, or delete the account.</summary>
    public class ProfileActivity : SecuredAppCompatActivity
    {
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

            await LoadProfileAsync();
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
            if (savedInstanceState == null)
            {
                var fragment = BottomNavFragment.NewInstance("profile");
                fragment.TabSelected += OnBottomNavTabSelected;

                SupportFragmentManager
                    .BeginTransaction()
                    .Replace(Resource.Id.frameBottomNav, fragment)
                    .Commit();
            }
        }

        /// <summary>Wires up click handlers for the edit, notifications, logout, and delete account buttons.</summary>
        private void SetupEventHandlers()
        {
            buttonProfileEdit.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(EditAccountActivity));
                StartActivityForResult(intent, EditAccountActivity.RequestCodeEditAccount);
            };

            buttonProfileNotifications.Click += (sender, e) => ToggleNotifications();
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

        /// <summary>
        /// Displays the user profile. Uses the in-memory session cache when available so
        /// re-opening the page does not trigger a server round-trip. The password count is
        /// always derived from the live vault cache so it stays accurate after vault edits.
        /// The cache is populated on the first load and invalidated whenever the account is
        /// edited (<see cref="OnActivityResult"/>).
        /// </summary>
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

        /// <summary>Populates the UI text views with the given user profile data.</summary>
        private void DisplayProfile(User profile)
        {
            textViewProfileUsername.Text = profile.Username ?? "—";
            textViewProfileEmail.Text = profile.Email ?? "—";
            textViewProfileLastLogin.Text = FormatDate(profile.LastLogin);
            textViewProfileLastPasswordChange.Text = FormatDate(profile.LastPasswordUpdate);
            textViewProfileCreatedAt.Text = FormatDate(profile.CreatedAt);
            // Always derive the count from the live vault so it stays accurate
            // after additions or deletions, regardless of what the server returned.
            textViewProfilePasswordCount.Text = SessionHelper.CachedVault.Count.ToString();
        }

        /// <summary>Formats a DateTime as a locale-friendly string, returning "—" for the minimum value.</summary>
        private string FormatDate(DateTime date)
        {
            if (date == DateTime.MinValue)
                return "—";

            return date.ToLocalTime().ToString("MMM dd, yyyy  h:mm tt");
        }

        /// <summary>Updates the notification button text to reflect the current notification preference.</summary>
        private void UpdateNotificationButton()
        {
            buttonProfileNotifications.Text = NotificationPreferenceHelper.IsEnabled
                ? GetString(Resource.String.profile_button_notifications_on)
                : GetString(Resource.String.profile_button_notifications_off);
        }

        /// <summary>Toggles the in-app notification preference and updates the button label.</summary>
        private void ToggleNotifications()
        {
            bool enabled = !NotificationPreferenceHelper.IsEnabled;
            NotificationPreferenceHelper.SetEnabled(enabled);
            buttonProfileNotifications.Text = enabled
                ? GetString(Resource.String.profile_button_notifications_on)
                : GetString(Resource.String.profile_button_notifications_off);
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
