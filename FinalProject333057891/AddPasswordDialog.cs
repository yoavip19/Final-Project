using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

namespace FinalProject333057891
{
    public class AddPasswordDialog : BottomSheetDialogFragment
    {
        // Event to notify when password is added
        public event EventHandler PasswordAdded;

        // UI
        AutoCompleteTextView actvAppSearch;
        TextView tvSearchOnline;
        LinearLayout layoutSelectedApp;
        ImageView ivSelectedAppIcon;
        TextView tvSelectedAppName;
        TextView tvChangeApp;

        EditText etAppUsername;
        EditText etPassword;
        EditText etConfirmPassword;

        Button btnTogglePassword;
        Button btnToggleConfirmPassword;
        Button btnAddPassword;

        CheckBox cbFavorite;

        // Errors
        TextView tvAppUsernameError;
        TextView tvPasswordError;
        TextView tvConfirmPasswordError;

        // State
        Application selectedApp;
        bool passwordVisible;
        bool confirmPasswordVisible;
        bool showingSearchResults = false;
        bool lockTextChanges = false; // NEW: Prevent TextChanged from interfering

        //DB
        static SQLiteConnection dbCommand;

        //SP
        ISharedPreferences sp;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.add_password_dialog_layout, container, false);

            #region FindViewById
            actvAppSearch = view.FindViewById<AutoCompleteTextView>(Resource.Id.actvAppSearch);
            tvSearchOnline = view.FindViewById<TextView>(Resource.Id.tvSearchOnline);

            layoutSelectedApp = view.FindViewById<LinearLayout>(Resource.Id.layoutSelectedApp);
            ivSelectedAppIcon = view.FindViewById<ImageView>(Resource.Id.ivSelectedAppIcon);
            tvSelectedAppName = view.FindViewById<TextView>(Resource.Id.tvSelectedAppName);
            tvChangeApp = view.FindViewById<TextView>(Resource.Id.tvChangeApp);

            etAppUsername = view.FindViewById<EditText>(Resource.Id.etAppUsername);
            etPassword = view.FindViewById<EditText>(Resource.Id.etPassword);
            etConfirmPassword = view.FindViewById<EditText>(Resource.Id.etConfirmPassword);

            btnTogglePassword = view.FindViewById<Button>(Resource.Id.btnTogglePassword);
            btnToggleConfirmPassword = view.FindViewById<Button>(Resource.Id.btnToggleConfirmPassword);
            btnAddPassword = view.FindViewById<Button>(Resource.Id.btnAddPassword);

            cbFavorite = view.FindViewById<CheckBox>(Resource.Id.cbFavorite);

            tvAppUsernameError = view.FindViewById<TextView>(Resource.Id.tvAppUsernameError);
            tvPasswordError = view.FindViewById<TextView>(Resource.Id.tvPasswordError);
            tvConfirmPasswordError = view.FindViewById<TextView>(Resource.Id.tvConfirmPasswordError);
            #endregion

            dbCommand = Helper.GetDBCommand(Activity);
            sp = Activity.GetSharedPreferences("details", FileCreationMode.Private);

            // Load local apps from database
            List<Application> localApps = LoadLocalApps();

            // Create adapter with filtering enabled for local search
            var adapter = new ApplicationAdapter(Activity, localApps, enableFiltering: true);
            actvAppSearch.Adapter = adapter;
            actvAppSearch.Threshold = 1;

            btnTogglePassword.Click += BtnTogglePassword_Click;
            btnToggleConfirmPassword.Click += BtnToggleConfirmPassword_Click;
            actvAppSearch.ItemClick += ActvAppSearch_ItemClick;
            tvChangeApp.Click += TvChangeApp_Click;
            btnAddPassword.Click += BtnAddPassword_Click;
            tvSearchOnline.Click += TvSearchOnline_Click;

            // Listen to text changes
            actvAppSearch.TextChanged += ActvAppSearch_TextChanged;

            tvSearchOnline.Visibility = ViewStates.Visible;

            return view;
        }

        private void ActvAppSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // CRITICAL FIX: Don't switch adapters if locked
            if (lockTextChanges)
            {
                return;
            }

            // If we were showing search results and user starts typing again
            if (showingSearchResults && !string.IsNullOrEmpty(actvAppSearch.Text))
            {
                // Switch back to local apps with filtering enabled
                List<Application> localApps = LoadLocalApps();
                var adapter = new ApplicationAdapter(Activity, localApps, enableFiltering: true);
                actvAppSearch.Adapter = adapter;
                showingSearchResults = false;
                tvSearchOnline.Visibility = ViewStates.Visible;
            }
        }

        private List<Application> LoadLocalApps()
        {
            try
            {
                return dbCommand.Table<Application>().ToList();
            }
            catch (Exception ex)
            {
                Toast.MakeText(Activity, "Error loading apps: " + ex.Message, ToastLength.Short).Show();
                return new List<Application>();
            }
        }

        private async void TvSearchOnline_Click(object sender, EventArgs e)
        {
            string searchQuery = actvAppSearch.Text?.Trim();

            if (string.IsNullOrEmpty(searchQuery))
            {
                Toast.MakeText(Activity, "Please enter an app name to search", ToastLength.Short).Show();
                return;
            }

            try
            {
                tvSearchOnline.Text = "Searching...";
                tvSearchOnline.Enabled = false;

                // CRITICAL FIX: Lock text changes while searching and showing results
                lockTextChanges = true;

                List<Application> onlineResults = await GooglePlayAPI.SearchAppsAsync(searchQuery, Activity);

                if (onlineResults != null && onlineResults.Count > 0)
                {

                    // Create adapter with filtering DISABLED for search results
                    var searchAdapter = new ApplicationAdapter(Activity, onlineResults, enableFiltering: false);
                    actvAppSearch.Adapter = searchAdapter;
                    showingSearchResults = true;

                    actvAppSearch.ShowDropDown();
                    tvSearchOnline.Visibility = ViewStates.Gone;

                    Toast.MakeText(Activity, $"Found {onlineResults.Count} apps", ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(Activity, "No apps found", ToastLength.Short).Show();
                    lockTextChanges = false; // Unlock if no results
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(Activity, "Search error: " + ex.Message, ToastLength.Long).Show();
                lockTextChanges = false; // Unlock on error
            }
            finally
            {
                tvSearchOnline.Text = "Search online";
                tvSearchOnline.Enabled = true;
            }
        }

        private void ActvAppSearch_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {

            try
            {
                // Get selected Application - validate position first
                var adapter = actvAppSearch.Adapter as ApplicationAdapter;
                if (adapter == null)
                {
                    Toast.MakeText(Activity, "Adapter error", ToastLength.Short).Show();
                    return;
                }

                if (e.Position < 0 || e.Position >= adapter.Count)
                {
                    Toast.MakeText(Activity, "Selection error", ToastLength.Short).Show();
                    return;
                }

                selectedApp = adapter[e.Position];

                // Update UI
                tvSelectedAppName.Text = selectedApp.AppName;

                try
                {
                    ivSelectedAppIcon.SetImageBitmap(Application.Base64ToBitmap(selectedApp.IconBase64));
                }
                catch (Exception ex)
                {
                    ivSelectedAppIcon.SetImageResource(Android.Resource.Drawable.IcMenuGallery);
                }

                // Hide search UI
                actvAppSearch.Visibility = ViewStates.Gone;
                tvSearchOnline.Visibility = ViewStates.Gone;
                layoutSelectedApp.Visibility = ViewStates.Visible;

                // CRITICAL FIX: Unlock text changes after selection is complete
                lockTextChanges = false;
            }
            catch (Exception ex)
            {
                Toast.MakeText(Activity, "Error: " + ex.Message, ToastLength.Long).Show();
                lockTextChanges = false; // Unlock on error
            }
        }

        private void BtnToggleConfirmPassword_Click(object sender, EventArgs e)
        {
            HideShowPassword(etConfirmPassword);
        }

        private void BtnTogglePassword_Click(object sender, EventArgs e)
        {
            HideShowPassword(etPassword);
        }

        private void HideShowPassword(EditText etPassword)
        {
            if (etPassword.InputType == (InputTypes.ClassText | InputTypes.TextVariationPassword))
                etPassword.InputType = InputTypes.ClassText;
            else
                etPassword.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
        }

        private void TvChangeApp_Click(object sender, EventArgs e)
        {
            selectedApp = null;
            showingSearchResults = false;
            lockTextChanges = false; // Unlock when starting fresh

            actvAppSearch.Text = "";
            actvAppSearch.Visibility = ViewStates.Visible;
            tvSearchOnline.Visibility = ViewStates.Visible;
            layoutSelectedApp.Visibility = ViewStates.Gone;

            // Reset to local apps with filtering
            List<Application> localApps = LoadLocalApps();
            var adapter = new ApplicationAdapter(Activity, localApps, enableFiltering: true);
            actvAppSearch.Adapter = adapter;
        }

        private void BtnAddPassword_Click(object sender, EventArgs e)
        {
            if (!IsValid())
                return;

            try
            {
                Password newAppPassword = new Password(
                    sp.GetString("Username", null),
                    selectedApp.PackageID,
                    sp.GetString("MasterPassword", null),
                    etAppUsername.Text,
                    etPassword.Text,
                    cbFavorite.Checked
                );

                int rowChange = dbCommand.Insert(newAppPassword);

                if (rowChange > 0)
                {
                    Toast.MakeText(Activity, "Password added successfully", ToastLength.Short).Show();
                    PasswordAdded?.Invoke(this, EventArgs.Empty);
                    Dismiss();
                }
                else
                {
                    Toast.MakeText(Activity, "Error adding password", ToastLength.Short).Show();
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(Activity, "Error: " + ex.Message, ToastLength.Short).Show();
            }
        }

        private bool IsValid()
        {
            bool isValid = true;

            // App must be selected
            if (selectedApp == null)
            {
                Toast.MakeText(Activity, "Please select an application", ToastLength.Short).Show();
                isValid = false;
            }

            // Username — must not be empty
            if (string.IsNullOrWhiteSpace(etAppUsername.Text))
            {
                tvAppUsernameError.Text = "Username cannot be empty";
                tvAppUsernameError.Visibility = ViewStates.Visible;
                isValid = false;
            }
            else
            {
                tvAppUsernameError.Visibility = ViewStates.Gone;
            }

            // Password — must not be empty
            if (string.IsNullOrWhiteSpace(etPassword.Text))
            {
                tvPasswordError.Text = "Password cannot be empty";
                tvPasswordError.Visibility = ViewStates.Visible;
                isValid = false;
            }
            else
            {
                tvPasswordError.Visibility = ViewStates.Gone;
            }

            // Confirm Password — must not be empty and must match
            if (string.IsNullOrWhiteSpace(etConfirmPassword.Text))
            {
                tvConfirmPasswordError.Text = "Please confirm your password";
                tvConfirmPasswordError.Visibility = ViewStates.Visible;
                isValid = false;
            }
            else if (etConfirmPassword.Text != etPassword.Text)
            {
                tvConfirmPasswordError.Text = "Passwords do not match";
                tvConfirmPasswordError.Visibility = ViewStates.Visible;
                isValid = false;
            }
            else
            {
                tvConfirmPasswordError.Visibility = ViewStates.Gone;
            }

            return isValid;
        }
    }
}