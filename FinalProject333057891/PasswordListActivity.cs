using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Text;

namespace FinalProject333057891
{
    [Activity(Label = "PasswordListActivity")]
    public class PasswordListActivity : BaseActivity
    {
        #region Properties
        Button btnAddPasswordToList;
        RecyclerView rvPasswordList;
        TextView tvEmptyState;
        PasswordListAdapter adapter;

        AndroidX.Fragment.App.Fragment flPasswordListMenuContainer;
        #endregion

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.password_list_layout);

            #region FindViewById
            btnAddPasswordToList = FindViewById<Button>(Resource.Id.btnAddPasswordToList);
            rvPasswordList = FindViewById<RecyclerView>(Resource.Id.rvPasswordList);
            tvEmptyState = FindViewById<TextView>(Resource.Id.tvEmptyState);

            SignedMenuFragment fragment = new SignedMenuFragment();
            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.flPasswordListMenuContainer, fragment)
                .Commit();
            #endregion

            #region Setup RecyclerView
            rvPasswordList.SetLayoutManager(new LinearLayoutManager(this));
            adapter = new PasswordListAdapter(new List<PasswordItem>());

            // Subscribe to adapter events
            adapter.ViewPasswordClicked += Adapter_ViewPasswordClicked;
            adapter.EditPasswordClicked += Adapter_EditPasswordClicked;
            adapter.DeletePasswordClicked += Adapter_DeletePasswordClicked;


            rvPasswordList.SetAdapter(adapter);
            #endregion

            #region Event Handlers
            btnAddPasswordToList.Click += BtnAddPasswordToList_Click;
            #endregion

            //TEMPORARY - Load popular apps for testing
            await LoadPopularApps();

            // Load passwords from database
            await LoadPasswordsAsync();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            // Reload passwords when returning to this activity
            await LoadPasswordsAsync();
        }

        /// <summary>
        /// Load all passwords for the current user from the database
        /// </summary>
        private async Task LoadPasswordsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    SQLiteConnection db = Helper.GetDBCommand(this);

                    // Get current username from SharedPreferences or session
                    var prefs = GetSharedPreferences("details", FileCreationMode.Private);
                    string currentUsername = prefs.GetString("Username", "");

                    if (string.IsNullOrEmpty(currentUsername))
                    {
                        RunOnUiThread(() =>
                        {
                            Toast.MakeText(this, "User not logged in", ToastLength.Short).Show();
                        });
                        return;
                    }

                    // Query all passwords for this user
                    var passwords = db.Table<Password>()
                        .Where(p => p.Username == currentUsername)
                        .ToList();

                    // Create PasswordItem objects by joining with Application table
                    List<PasswordItem> passwordItems = new List<PasswordItem>();

                    foreach (var password in passwords)
                    {
                        var app = db.Find<Application>(password.AppPackageID);
                        if (app != null)
                        {
                            passwordItems.Add(new PasswordItem(password, app));
                        }
                    }

                    // Update UI on main thread
                    RunOnUiThread(() =>
                    {
                        adapter.UpdateData(passwordItems);

                        // Show/hide empty state
                        if (passwordItems.Count == 0)
                        {
                            rvPasswordList.Visibility = ViewStates.Gone;
                            tvEmptyState.Visibility = ViewStates.Visible;
                        }
                        else
                        {
                            rvPasswordList.Visibility = ViewStates.Visible;
                            tvEmptyState.Visibility = ViewStates.Gone;
                        }
                    });
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        Toast.MakeText(this, "Error loading passwords: " + ex.Message, ToastLength.Long).Show();
                    });
                }
            });
        }

        /// <summary>
        /// Handle view Password button click
        /// </summary>
        private void Adapter_ViewPasswordClicked(object sender, PasswordItem item)
        {
            // Show master password confirmation before viewing
            ShowMasterPasswordConfirmationForView(item);
        }

        /// <summary>
        /// Show master password confirmation dialog before viewing password
        /// </summary>
        private void ShowMasterPasswordConfirmationForView(PasswordItem item)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Confirm Master Password");
            builder.SetMessage($"Enter your master password to view the password for {item.AppName}:");

            // Create EditText for master password
            EditText input = new EditText(this);
            input.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
            input.Hint = "Master Password";

            // Add some padding to the input
            var layoutParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent,
                LinearLayout.LayoutParams.WrapContent);
            layoutParams.SetMargins(50, 20, 50, 20);
            input.LayoutParameters = layoutParams;

            builder.SetView(input);

            builder.SetPositiveButton("Confirm", (s, e) =>
            {
                string masterPassword = input.Text;
                VerifyMasterPasswordAndView(masterPassword, item);
            });

            builder.SetNegativeButton("Cancel", (s, e) => { });

            builder.Show();
        }

        /// <summary>
        /// Verify master password, decrypt password, and show view dialog
        /// </summary>
        private void VerifyMasterPasswordAndView(string masterPassword, PasswordItem item)
        {
            try
            {
                SQLiteConnection db = Helper.GetDBCommand(this);

                // Get current user from database
                var sp = GetSharedPreferences("details", FileCreationMode.Private);
                string username = sp.GetString("Username", "");

                var user = db.Find<User>(username);

                if (user == null)
                {
                    Toast.MakeText(this, "User not found", ToastLength.Short).Show();
                    return;
                }

                // Verify master password
                string hashedInput = SecurityHelper.HashPassword(masterPassword, user.MasterSalt);
                if (hashedInput != user.MasterPasswordHash)
                {
                    Toast.MakeText(this, "Incorrect master password", ToastLength.Long).Show();
                    return;
                }

                // Master password verified - decrypt the password
                string decryptedPassword = SecurityHelper.DecryptAES(
                    item.PasswordEncrypted,
                    masterPassword,
                    item.Salt,
                    item.InitVector
                );

                // Open view dialog with decrypted password
                var viewPasswordDialog = new ViewPasswordDialog(item, decryptedPassword);
                viewPasswordDialog.Show(SupportFragmentManager, "view_password");

                // CRITICAL SECURITY: Clear decrypted password from local variable
                // It will be managed by the dialog now
                decryptedPassword = null;
                GC.Collect();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Long).Show();
            }
        }

        /// <summary>
        /// Handle Edit Password button click
        /// </summary>
        private void Adapter_EditPasswordClicked(object sender, PasswordItem item)
        {
            var editDialog = new EditPasswordDialog(item);

            // Subscribe to the PasswordUpdated event to refresh the list
            editDialog.PasswordUpdated += async (s, e) => await LoadPasswordsAsync();

            editDialog.Show(SupportFragmentManager, "edit_password");
        }

        /// <summary>
        /// Handle Delete Password button click — shows confirmation dialog before deleting
        /// </summary>
        /// <summary>
        /// Handle Delete Password button click — prompt for master password first
        /// </summary>
        private void Adapter_DeletePasswordClicked(object sender, PasswordItem item)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Confirm Master Password");
            builder.SetMessage($"Enter your master password to delete the password for {item.AppName}:");

            EditText input = new EditText(this);
            input.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
            input.Hint = "Master Password";

            var layoutParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent,
                LinearLayout.LayoutParams.WrapContent);
            layoutParams.SetMargins(50, 20, 50, 20);
            input.LayoutParameters = layoutParams;

            builder.SetView(input);

            builder.SetPositiveButton("Confirm", (s, e) =>
            {
                VerifyMasterPasswordAndDelete(input.Text, item);
            });

            builder.SetNegativeButton("Cancel", (s, e) => { });
            builder.Show();
        }

        /// <summary>
        /// Verify master password, then show final confirmation before deleting
        /// </summary>
        private void VerifyMasterPasswordAndDelete(string masterPassword, PasswordItem item)
        {
            try
            {
                SQLiteConnection db = Helper.GetDBCommand(this);
                var sp = GetSharedPreferences("details", FileCreationMode.Private);
                string username = sp.GetString("Username", "");

                var user = db.Find<User>(username);
                if (user == null)
                {
                    Toast.MakeText(this, "User not found", ToastLength.Short).Show();
                    return;
                }

                string hashedInput = SecurityHelper.HashPassword(masterPassword, user.MasterSalt);
                if (hashedInput != user.MasterPasswordHash)
                {
                    Toast.MakeText(this, "Incorrect master password", ToastLength.Long).Show();
                    return;
                }

                // Master password verified — show final confirmation
                AlertDialog.Builder confirmBuilder = new AlertDialog.Builder(this);
                confirmBuilder.SetTitle("Delete Password");
                confirmBuilder.SetMessage($"Are you sure you want to delete the password for {item.AppName}? This cannot be undone.");

                confirmBuilder.SetPositiveButton("Delete", (s, e) =>
                {
                    try
                    {
                        int rowsDeleted = db.Execute(
                            "DELETE FROM Passwords WHERE Username = ? AND AppPackageID = ?",
                            item.Username,
                            item.AppPackageID
                        );

                        if (rowsDeleted > 0)
                        {
                            Toast.MakeText(this, $"{item.AppName} password deleted.", ToastLength.Short).Show();
                            _ = LoadPasswordsAsync();
                        }
                        else
                        {
                            Toast.MakeText(this, "Failed to delete password.", ToastLength.Short).Show();
                        }
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, "Error deleting password: " + ex.Message, ToastLength.Long).Show();
                    }
                });

                confirmBuilder.SetNegativeButton("Cancel", (s, e) => { });
                confirmBuilder.Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private void BtnAddPasswordToList_Click(object sender, EventArgs e)
        {
            var dialog = new AddPasswordDialog();

            // Subscribe to the PasswordAdded event to refresh the list
            dialog.PasswordAdded += async (s, args) => await LoadPasswordsAsync();

            dialog.Show(SupportFragmentManager, "add_password");
        }

        //===========================================================================================================
        // TEMPORARY CODE FOR TESTING
        //===========================================================================================================

        private async Task LoadPopularApps()
        {
            var popularApps = new List<string>
            {
                "com.whatsapp",                // WhatsApp
                "com.instagram.android",       // Instagram
                "com.facebook.katana",         // Facebook
                "com.zhiliaoapp.musically",    // TikTok
                "com.snapchat.android",        // Snapchat
                "org.telegram.messenger",      // Telegram
                "com.spotify.music",           // Spotify
                "com.netflix.mediaclient",     // Netflix
                "com.google.android.youtube",  // YouTube
                "com.twitter.android",         // Twitter / X
                "com.waze",                    // Waze
                "com.google.android.gm",       // Gmail
                "com.amazon.mShop.android.shopping", // Amazon Shopping
                "com.discord",                 // Discord
                "com.pinterest",               // Pinterest
                "com.linkedin.android",        // LinkedIn
                "com.ubercab",                 // Uber
                "com.paypal.android.p2pmobile",// PayPal
                "com.microsoft.teams",         // Microsoft Teams
                "zoom.us.google"               // Zoom
            };

            foreach (var packageId in popularApps)
            {
                await AddAppSafeAsync(packageId);
            }
        }

        private async Task AddAppSafeAsync(string packageId)
        {
            try
            {
                SQLiteConnection dbCommand = Helper.GetDBCommand(this);
                var app = await GooglePlayAPI.GetAppMetadataAsync(packageId);

                if (app == null) return;

                if (dbCommand.Find<Application>(app.PackageID) == null)
                {
                    dbCommand.Insert(app);
                }
            }
            catch
            {
                return;
            }
        }
    }
}