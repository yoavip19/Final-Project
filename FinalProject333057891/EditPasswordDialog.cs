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
    public class EditPasswordDialog : BottomSheetDialogFragment
    {
        // UI Elements
        ImageView ivEditAppIcon;
        TextView tvEditAppName;
        EditText etEditAppUsername;
        EditText etEditPassword;
        EditText etEditConfirmPassword;
        Button btnEditTogglePassword;
        Button btnEditToggleConfirmPassword;
        Button btnSavePasswordChanges;
        CheckBox cbEditFavorite;

        // Error TextViews
        TextView tvEditAppUsernameError;
        TextView tvEditPasswordError;
        TextView tvEditConfirmPasswordError;

        // Data
        private PasswordItem passwordItem;

        // DB
        static SQLiteConnection dbCommand;

        // SP
        ISharedPreferences sp;

        // Event to notify when password is updated
        public event EventHandler PasswordUpdated;

        // Constructor
        public EditPasswordDialog(PasswordItem item)
        {
            passwordItem = item;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.edit_password_dialog_layout, container, false);

            #region FindViewById
            ivEditAppIcon = view.FindViewById<ImageView>(Resource.Id.ivEditAppIcon);
            tvEditAppName = view.FindViewById<TextView>(Resource.Id.tvEditAppName);
            etEditAppUsername = view.FindViewById<EditText>(Resource.Id.etEditAppUsername);
            etEditPassword = view.FindViewById<EditText>(Resource.Id.etEditPassword);
            etEditConfirmPassword = view.FindViewById<EditText>(Resource.Id.etEditConfirmPassword);
            btnEditTogglePassword = view.FindViewById<Button>(Resource.Id.btnEditTogglePassword);
            btnEditToggleConfirmPassword = view.FindViewById<Button>(Resource.Id.btnEditToggleConfirmPassword);
            btnSavePasswordChanges = view.FindViewById<Button>(Resource.Id.btnSavePasswordChanges);
            cbEditFavorite = view.FindViewById<CheckBox>(Resource.Id.cbEditFavorite);

            tvEditAppUsernameError = view.FindViewById<TextView>(Resource.Id.tvEditAppUsernameError);
            tvEditPasswordError = view.FindViewById<TextView>(Resource.Id.tvEditPasswordError);
            tvEditConfirmPasswordError = view.FindViewById<TextView>(Resource.Id.tvEditConfirmPasswordError);
            #endregion

            dbCommand = Helper.GetDBCommand(Activity);
            sp = Activity.GetSharedPreferences("details", FileCreationMode.Private);

            // Populate fields with existing data
            PopulateFields();

            // Event handlers
            btnEditTogglePassword.Click += BtnEditTogglePassword_Click;
            btnEditToggleConfirmPassword.Click += BtnEditToggleConfirmPassword_Click;
            btnSavePasswordChanges.Click += BtnSavePasswordChanges_Click;

            return view;
        }

        private void PopulateFields()
        {
            // Set app icon and name (read-only)
            tvEditAppName.Text = passwordItem.AppName;
            try
            {
                ivEditAppIcon.SetImageBitmap(passwordItem.GetIconBitmap());
            }
            catch
            {
                ivEditAppIcon.SetImageResource(Android.Resource.Drawable.IcMenuGallery);
            }

            // Set current app username
            etEditAppUsername.Text = passwordItem.AppUsername;

            // Set favorite status
            cbEditFavorite.Checked = passwordItem.IsFavorite;

            // Note: We don't populate the password fields - user must enter new password
            // Alternatively, you could decrypt and show current password, but this requires master password
        }

        private void BtnEditTogglePassword_Click(object sender, EventArgs e)
        {
            HideShowPassword(etEditPassword);
        }

        private void BtnEditToggleConfirmPassword_Click(object sender, EventArgs e)
        {
            HideShowPassword(etEditConfirmPassword);
        }

        private void HideShowPassword(EditText etPassword)
        {
            if (etPassword.InputType == (InputTypes.ClassText | InputTypes.TextVariationPassword))
                etPassword.InputType = InputTypes.ClassText;
            else
                etPassword.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
        }

        private void BtnSavePasswordChanges_Click(object sender, EventArgs e)
        {
            // Clear previous errors
            ClearErrors();

            // Validate inputs
            if (!IsValid())
                return;

            // Show master password confirmation dialog before saving
            ShowMasterPasswordConfirmation();
        }

        private void ClearErrors()
        {
            tvEditAppUsernameError.Visibility = ViewStates.Gone;
            tvEditPasswordError.Visibility = ViewStates.Gone;
            tvEditConfirmPasswordError.Visibility = ViewStates.Gone;
        }

        private bool IsValid()
        {
            bool isValid = true;

            // Username — must not be empty
            if (string.IsNullOrWhiteSpace(etEditAppUsername.Text))
            {
                tvEditAppUsernameError.Text = "Username cannot be empty";
                tvEditAppUsernameError.Visibility = ViewStates.Visible;
                isValid = false;
            }
            else
            {
                tvEditAppUsernameError.Visibility = ViewStates.Gone;
            }

            string newPassword = etEditPassword.Text;
            string confirmPassword = etEditConfirmPassword.Text;

            // Only validate password fields if the user entered something
            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    tvEditPasswordError.Text = "Please enter a new password";
                    tvEditPasswordError.Visibility = ViewStates.Visible;
                    isValid = false;
                }
                else
                {
                    tvEditPasswordError.Visibility = ViewStates.Gone;
                }

                if (string.IsNullOrWhiteSpace(confirmPassword))
                {
                    tvEditConfirmPasswordError.Text = "Confirm password cannot be empty";
                    tvEditConfirmPasswordError.Visibility = ViewStates.Visible;
                    isValid = false;
                }
            }
            else if (!string.IsNullOrWhiteSpace(newPassword) && newPassword != confirmPassword)
            {
                tvEditConfirmPasswordError.Text = "Passwords do not match";
                tvEditConfirmPasswordError.Visibility = ViewStates.Visible;
                isValid = false;
            }
            else
            {
                tvEditConfirmPasswordError.Visibility = ViewStates.Gone;
            }

            return isValid;
        }

        private void ShowMasterPasswordConfirmation()
        {
            // Create a dialog to ask for master password
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity);
            builder.SetTitle("Confirm Master Password");
            builder.SetMessage("Please enter your master password to save changes:");

            // Create EditText for master password
            EditText input = new EditText(Activity);
            input.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
            input.Hint = "Master Password";
            builder.SetView(input);

            builder.SetPositiveButton("Confirm", (s, e) =>
            {
                string masterPassword = input.Text;
                VerifyMasterPasswordAndSave(masterPassword);
            });

            builder.SetNegativeButton("Cancel", (s, e) => { });

            builder.Show();
        }

        private void VerifyMasterPasswordAndSave(string masterPassword)
        {
            try
            {
                //// Get current user from database
                //string username = sp.GetString("Username", "");
                //var user = dbCommand.Find<User>(username);

                //if (user == null)
                //{
                //    Toast.MakeText(Activity, "User not found", ToastLength.Short).Show();
                //    return;
                //}

                // Verify master password
                //string hashedInput = SecurityHelper.HashPassword(masterPassword, user.MasterSalt);
                //if (hashedInput != user.MasterPasswordHash)
                //{
                //    Toast.MakeText(Activity, "Incorrect master password", ToastLength.Short).Show();
                //    return;
                //}

                string originalPassword = sp.GetString("MasterPassword", null); //FIX THIS - storing master password in shared preferences is not secure
                if (originalPassword != masterPassword)
                {
                    Toast.MakeText(Activity, "Incorrect master password", ToastLength.Short).Show();
                    return;
                }

                // Master password verified - now save changes
                SavePasswordChanges(masterPassword);
            }
            catch (Exception ex)
            {
                Toast.MakeText(Activity, "Error verifying password: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private void SavePasswordChanges(string masterPassword)
        {
            try
            {
                // Update fields
                string newAppUsername = etEditAppUsername.Text;
                bool newIsFavorite = cbEditFavorite.Checked;

                string newEncryptedPassword = null;
                string newSalt = null;
                string newIV = null;

                // Only update password if user entered a new one
                if (!string.IsNullOrEmpty(etEditPassword.Text))
                {
                    newEncryptedPassword = SecurityHelper.EncryptAES(
                        etEditPassword.Text,
                        masterPassword,
                        out newSalt,
                        out newIV
                    );
                }
                else
                {
                    // Keep existing encrypted password
                    newEncryptedPassword = passwordItem.PasswordEncrypted;
                    newSalt = passwordItem.Salt;
                    newIV = passwordItem.InitVector;
                }

                // Use raw SQL UPDATE query to handle composite primary key
                string updateQuery = @"UPDATE Passwords 
                              SET AppUsername = ?, 
                                  PasswordEncrypted = ?, 
                                  Salt = ?, 
                                  InitVector = ?, 
                                  IsFavorite = ? 
                              WHERE Username = ? AND AppPackageID = ?";

                int rowsUpdated = dbCommand.Execute(
                    updateQuery,
                    newAppUsername,
                    newEncryptedPassword,
                    newSalt,
                    newIV,
                    newIsFavorite ? 1 : 0, // SQLite uses 1/0 for boolean
                    passwordItem.Username,
                    passwordItem.AppPackageID
                );

                if (rowsUpdated > 0)
                {
                    Toast.MakeText(Activity, "Password updated successfully", ToastLength.Short).Show();

                    // Notify parent activity to refresh the list
                    PasswordUpdated?.Invoke(this, EventArgs.Empty);

                    Dismiss();
                }
                else
                {
                    Toast.MakeText(Activity, "Failed to update password", ToastLength.Short).Show();
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(Activity, "Error saving changes: " + ex.Message, ToastLength.Long).Show();
            }
        }
    }
}