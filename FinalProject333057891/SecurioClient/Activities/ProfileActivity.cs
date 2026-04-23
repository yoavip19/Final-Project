using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
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
    public class ProfileActivity : AppCompatActivity
    {
        private TextView textViewProfileUsername;
        private TextView textViewProfileEmail;
        private TextView textViewProfileLastLogin;
        private TextView textViewProfileLastPasswordChange;
        private TextView textViewProfileCreatedAt;
        private TextView textViewProfilePasswordCount;
        private MaterialButton buttonProfileEdit;
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
            buttonProfileLogout = FindViewById<MaterialButton>(Resource.Id.buttonProfileLogout);
            buttonProfileDelete = FindViewById<MaterialButton>(Resource.Id.buttonProfileDelete);
            progressBarProfile = FindViewById<ProgressBar>(Resource.Id.progressBarProfile);
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

        /// <summary>Wires up click handlers for the edit, logout, and delete account buttons.</summary>
        private void SetupEventHandlers()
        {
            buttonProfileEdit.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(EditAccountActivity));
                StartActivityForResult(intent, EditAccountActivity.RequestCodeEditAccount);
            };

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
                // Refresh the displayed profile after a successful account edit.
                await LoadProfileAsync();
            }
        }

        /// <summary>Fetches the user profile from the server and displays it, falling back to cached data on failure.</summary>
        private async Task LoadProfileAsync()
        {
            progressBarProfile.Visibility = ViewStates.Visible;

            try
            {
                var profileService = new ProfileService();
                var result = await profileService.GetProfileAsync();

                if (result.Success && result.Data != null)
                {
                    DisplayProfile(result.Data);
                }
                else
                {
                    // Fall back to cached profile data
                    var cached = await StorageHelper.GetCachedProfileAsync();
                    if (cached != null)
                    {
                        DisplayProfile(cached);
                        Toast.MakeText(this, Resource.String.profile_load_error, ToastLength.Short).Show();
                    }
                    else
                    {
                        Toast.MakeText(this, result.Message ?? "Unable to load profile.", ToastLength.Long).Show();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PROFILE LOAD ERROR] {ex.Message}");

                // Fall back to cached profile data
                var cached = await StorageHelper.GetCachedProfileAsync();
                if (cached != null)
                {
                    DisplayProfile(cached);
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
            textViewProfilePasswordCount.Text = profile.PasswordCount.ToString();
        }

        /// <summary>Formats a DateTime as a locale-friendly string, returning "—" for the minimum value.</summary>
        private string FormatDate(DateTime date)
        {
            if (date == DateTime.MinValue)
                return "—";

            return date.ToLocalTime().ToString("MMM dd, yyyy  h:mm tt");
        }

        /// <summary>Shows a confirmation dialog before logging out.</summary>
        private void ConfirmLogout()
        {
            new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.profile_logout_confirm_title)
                .SetMessage(Resource.String.profile_logout_confirm_message)
                .SetPositiveButton(Resource.String.profile_logout_confirm_yes, (s, e) =>
                {
                    PerformLogout();
                })
                .SetNegativeButton(Resource.String.profile_logout_confirm_no, (s, e) => { })
                .Show();
        }

        /// <summary>Clears the session and navigates to LoginActivity.</summary>
        private async void PerformLogout()
        {
            SessionHelper.EndSession();
            await StorageHelper.ClearSessionAsync();

            var intent = new Intent(this, typeof(LoginActivity));
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            StartActivity(intent);
            Finish();
        }

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PROFILE DELETE ERROR] {ex.Message}");
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
