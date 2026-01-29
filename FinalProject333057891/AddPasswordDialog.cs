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
        string selectedAppName;
        string selectedPackageId;
        string selectedIconBase64;
        bool passwordVisible;
        bool confirmPasswordVisible;

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

            List<AppSuggestion> suggestions = LoadSuggestions();

            var adapter = new AppSuggestionAdapter(Activity, suggestions);
            actvAppSearch.Adapter = adapter;
            actvAppSearch.Threshold = 1; // start showing suggestions after 1 character

            btnTogglePassword.Click += BtnTogglePassword_Click;
            btnToggleConfirmPassword.Click += BtnToggleConfirmPassword_Click;

            actvAppSearch.ItemClick += ActvAppSearch_ItemClick;

            tvChangeApp.Click += TvChangeApp_Click;

            btnAddPassword.Click += BtnAddPassword_Click;

            return view;
        }

        private List<AppSuggestion> LoadSuggestions()
        {
            try
            {
                string query = "SELECT PackageID, AppName, IconBase64 FROM Applications;";
                List<Application> apps = dbCommand.Query<Application>(query);
                List<AppSuggestion> appSuggestions = new List<AppSuggestion>();
                foreach (Application app in apps)
                {
                    appSuggestions.Add(new AppSuggestion() { AppName = app.AppName, PackageID = app.PackageID, IconBase64 = app.IconBase64 });
                }
                return appSuggestions;
            }
            catch (Exception ex)
            {
                Toast.MakeText(Activity, "Code error:\n" + ex.ToString(), ToastLength.Short).Show();
                return null;
            }
        }

        private void ActvAppSearch_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            AppSuggestion app = ((AppSuggestionAdapter)actvAppSearch.Adapter)[e.Position];
            selectedAppName = app.AppName;
            selectedPackageId = app.PackageID;
            selectedIconBase64 = app.IconBase64;

            tvSelectedAppName.Text = app.AppName;
            ivSelectedAppIcon.SetImageBitmap(Application.Base64ToBitmap(selectedIconBase64));

            actvAppSearch.Visibility = ViewStates.Gone;
            tvSearchOnline.Visibility = ViewStates.Gone;
            layoutSelectedApp.Visibility = ViewStates.Visible;
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
            //handles the hide/show password button
            if (etPassword.InputType == (InputTypes.ClassText | InputTypes.TextVariationPassword))
                etPassword.InputType = InputTypes.ClassText;
            else
                etPassword.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
        }
        private void TvChangeApp_Click(object sender, EventArgs e)
        {
            selectedAppName = "";
            selectedPackageId = "";
            selectedIconBase64 = "";

            actvAppSearch.Text = "";
            actvAppSearch.Visibility = ViewStates.Visible;
            layoutSelectedApp.Visibility = ViewStates.Gone;
        }

        private void BtnAddPassword_Click(object sender, EventArgs e)
        {
            //If(IsValid)
            //{

            //Add to DB
            try
            {
                Password newAppPassword = new Password(sp.GetString("Username",null), selectedPackageId, sp.GetString("MasterPassword", null), etAppUsername.Text, etPassword.Text, cbFavorite.Checked);
                int rowChange = dbCommand.Insert(newAppPassword);
                if (rowChange > 0)
                {
                    Toast.MakeText(Activity, "Password added successfully", ToastLength.Short).Show();
                    Dismiss();
                }
                else
                {
                    Toast.MakeText(Activity, "Error with adding", ToastLength.Short).Show();
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(Activity, "Code error:\n" + ex.ToString(), ToastLength.Short).Show();
                return;
            }

            //}
        }
    }
}