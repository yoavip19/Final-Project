using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurioClient.Activities
{
    /// <summary>Activity that allows the user to update their username, email, and optionally their master password.</summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class EditAccountActivity : SecuredAppCompatActivity
    {
        // Request code used when launching this activity from ProfileActivity.
        public const int RequestCodeEditAccount = 2001;
        // Result key indicating the profile was updated.
        public const string ResultUpdated = "AccountUpdated";

        private TextInputEditText editTextUsername;
        private TextInputEditText editTextEmail;
        private TextInputEditText editTextCurrentPassword;
        private TextInputEditText editTextNewPassword;
        private TextInputEditText editTextConfirmNewPassword;
        private MaterialButton buttonSave;
        private MaterialButton buttonGeneratePassword;
        private ImageView imageViewBack;
        private TextView textViewUsernameError;
        private TextView textViewEmailError;
        private TextView textViewCurrentPasswordError;
        private TextView textViewNewPasswordError;
        private ProgressBar progressBarPasswordStrength;
        private TextView textViewPasswordHint;
        private TextView textViewConfirmNewPasswordError;
        private TextView textViewGeneralError;
        private ProgressBar progressBarEditAccount;

        // The current values loaded from the profile.
        private string originalUsername;
        private string originalEmail;

        /// <summary>Initializes the activity, inflates the layout, and pre-fills fields with the current profile.</summary>
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_edit_account);

            InitializeViews();
            SetupEventHandlers();
            await LoadCurrentProfileAsync();
        }

        /// <summary>Finds and assigns all view references from the layout.</summary>
        private void InitializeViews()
        {
            editTextUsername            = FindViewById<TextInputEditText>(Resource.Id.editTextEditUsername);
            editTextEmail               = FindViewById<TextInputEditText>(Resource.Id.editTextEditEmail);
            editTextCurrentPassword     = FindViewById<TextInputEditText>(Resource.Id.editTextCurrentPassword);
            editTextNewPassword         = FindViewById<TextInputEditText>(Resource.Id.editTextNewPassword);
            editTextConfirmNewPassword  = FindViewById<TextInputEditText>(Resource.Id.editTextConfirmNewPassword);
            buttonSave                  = FindViewById<MaterialButton>(Resource.Id.buttonEditSave);
            buttonGeneratePassword      = FindViewById<MaterialButton>(Resource.Id.buttonEditGeneratePassword);
            imageViewBack               = FindViewById<ImageView>(Resource.Id.imageViewEditAccountBack);
            textViewUsernameError       = FindViewById<TextView>(Resource.Id.textViewEditUsernameError);
            textViewEmailError          = FindViewById<TextView>(Resource.Id.textViewEditEmailError);
            textViewCurrentPasswordError = FindViewById<TextView>(Resource.Id.textViewCurrentPasswordError);
            textViewNewPasswordError    = FindViewById<TextView>(Resource.Id.textViewNewPasswordError);
            progressBarPasswordStrength = FindViewById<ProgressBar>(Resource.Id.progressBarEditPasswordStrength);
            textViewPasswordHint        = FindViewById<TextView>(Resource.Id.textViewEditPasswordHint);
            textViewConfirmNewPasswordError = FindViewById<TextView>(Resource.Id.textViewConfirmNewPasswordError);
            textViewGeneralError        = FindViewById<TextView>(Resource.Id.textViewEditGeneralError);
            progressBarEditAccount      = FindViewById<ProgressBar>(Resource.Id.progressBarEditAccount);
        }

        /// <summary>Wires up click and text-change event handlers for the edit account form controls.</summary>
        private void SetupEventHandlers()
        {
            imageViewBack.Click += (sender, e) => Finish();

            buttonSave.Click += async (sender, e) => await OnSaveClicked();

            buttonGeneratePassword.Click += (sender, e) =>
            {
                string generated = ValidationHelper.GenerateStrongPassword();
                editTextNewPassword.Text = generated;
            };

            // Real-time validation — reuses the same ValidationHelper as signup (DRY).
            editTextUsername.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidateUsername(editTextUsername.Text?.Trim());
                if (!result.IsValid) FormUiHelper.ShowError(textViewUsernameError, result.ErrorMessage);
                else                 FormUiHelper.HideError(textViewUsernameError);
            }));

            editTextEmail.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidateEmail(editTextEmail.Text?.Trim());
                if (!result.IsValid) FormUiHelper.ShowError(textViewEmailError, result.ErrorMessage);
                else                 FormUiHelper.HideError(textViewEmailError);
            }));

            editTextNewPassword.AddTextChangedListener(new SimpleTextWatcher(text =>
            {
                FormUiHelper.UpdatePasswordStrengthIndicator(
                    text, progressBarPasswordStrength, textViewPasswordHint, textViewNewPasswordError, Resources);

                if (!string.IsNullOrEmpty(editTextConfirmNewPassword.Text))
                {
                    var matchResult = ValidationHelper.ValidatePasswordsMatch(text, editTextConfirmNewPassword.Text);
                    if (!matchResult.IsValid) FormUiHelper.ShowError(textViewConfirmNewPasswordError, matchResult.ErrorMessage);
                    else                      FormUiHelper.HideError(textViewConfirmNewPasswordError);
                }
            }));

            editTextConfirmNewPassword.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidatePasswordsMatch(
                    editTextNewPassword.Text, editTextConfirmNewPassword.Text);
                if (!result.IsValid) FormUiHelper.ShowError(textViewConfirmNewPasswordError, result.ErrorMessage);
                else                 FormUiHelper.HideError(textViewConfirmNewPasswordError);
            }));
        }

        /// <summary>Loads the current username and email from the server or cache and pre-fills the form fields.</summary>
        private async Task LoadCurrentProfileAsync()
        {
            try
            {
                var profileService = new ProfileService();
                var result = await profileService.GetProfileAsync();

                if (result.Success && result.Data != null)
                {
                    originalUsername = result.Data.Username;
                    originalEmail = result.Data.Email;
                }
                else
                {
                    var cached = await StorageHelper.GetCachedProfileAsync();
                    if (cached != null)
                    {
                        originalUsername = cached.Username;
                        originalEmail = cached.Email;
                    }
                }
            }
            catch
            {
                var cached = await StorageHelper.GetCachedProfileAsync();
                if (cached != null)
                {
                    originalUsername = cached.Username;
                    originalEmail = cached.Email;
                }
            }

            editTextUsername.Text = originalUsername ?? "";
            editTextEmail.Text = originalEmail ?? "";
        }

        /// <summary>Validates inputs, re-encrypts vault items if the password changes, and submits the update.</summary>
        private async Task OnSaveClicked()
        {
            ClearErrors();

            string username        = editTextUsername.Text?.Trim();
            string email           = editTextEmail.Text?.Trim();
            string currentPassword = editTextCurrentPassword.Text;
            string newPassword     = editTextNewPassword.Text;
            string confirmPassword = editTextConfirmNewPassword.Text;

            // Determine if the user is changing their master password.
            bool isChangingPassword = !string.IsNullOrEmpty(currentPassword) ||
                                     !string.IsNullOrEmpty(newPassword) ||
                                     !string.IsNullOrEmpty(confirmPassword);

            if (!ValidateInputs(username, email, currentPassword, newPassword, confirmPassword, isChangingPassword))
                return;

            SetLoadingState(true);

            try
            {
                var request = new UpdateAccountRequest
                {
                    Username = username,
                    Email = email,
                    PasswordChanged = isChangingPassword
                };

                if (isChangingPassword)
                {
                    // HIBP breach check — done entirely on the client; the SHA-1 hash never reaches the server.
                    bool isPwned = await HibpClientService.IsPasswordPwnedAsync(
                        EncryptionHelper.ComputeSha1Hash(newPassword));
                    if (isPwned)
                    {
                        FormUiHelper.ShowError(textViewNewPasswordError,
                            "This password has appeared in a known data breach. Please choose a different password.");
                        return;
                    }

                    // Verify the current password by deriving the auth key and comparing locally.
                    // We fetch the user's salts from the server to derive the current key.
                    var authService = new AuthService();
                    var saltResult = await authService.GetSaltsAsync(email);

                    // If salts aren't available for the current email (e.g., email changed), use original email.
                    if (!saltResult.Success)
                        saltResult = await authService.GetSaltsAsync(originalEmail);

                    if (!saltResult.Success)
                    {
                        FormUiHelper.ShowError(textViewGeneralError, GetString(Resource.String.edit_account_verify_password_failed));
                        return;
                    }

                    var (currentAuthKey,  currentVaultKey) = await Task.Run(() => (
                        EncryptionHelper.DeriveKey(currentPassword, saltResult.Data.AuthSalt),
                        EncryptionHelper.DeriveKey(currentPassword, saltResult.Data.EncryptionSalt)
                    ));

                    // Verify the current vault key matches the session key.
                    if (currentVaultKey != SessionHelper.SessionVaultKey)
                    {
                        FormUiHelper.ShowError(textViewCurrentPasswordError, GetString(Resource.String.edit_account_current_password_wrong));
                        return;
                    }

                    // No-reuse check: compare new password against the current one and last 4 history entries.
                    // For each entry (and the current), derive: DeriveKey(newPassword, entry.AuthSalt).
                    // A match means the password was already used → reject.
                    string newKeyForReuseCheck = await Task.Run(() => EncryptionHelper.DeriveKey(newPassword, saltResult.Data.AuthSalt));
                    if (newKeyForReuseCheck == currentAuthKey)
                    {
                        FormUiHelper.ShowError(textViewNewPasswordError, GetString(Resource.String.edit_account_password_reused));
                        return;
                    }

                    var historyService = new ProfileService();
                    var historyResult = await historyService.GetPasswordHistoryAsync();
                    if (historyResult.Success && historyResult.Data != null)
                    {
                        bool isReused = await Task.Run(() =>
                        {
                            foreach (var entry in historyResult.Data)
                            {
                                string historicCheck = EncryptionHelper.DeriveKey(newPassword, entry.AuthSalt);
                                if (historicCheck == entry.PasswordKey) return true;
                            }
                            return false;
                        });

                        if (isReused)
                        {
                            FormUiHelper.ShowError(textViewNewPasswordError, GetString(Resource.String.edit_account_password_reused));
                            return;
                        }
                    }

                    // Generate new salts and derive new keys.
                    string newAuthSalt       = EncryptionHelper.GenerateSalt();
                    string newEncryptionSalt = EncryptionHelper.GenerateSalt();
                    var (newMasterPasswordKey, newVaultKey) = await Task.Run(() => (
                        EncryptionHelper.DeriveKey(newPassword, newAuthSalt),
                        EncryptionHelper.DeriveKey(newPassword, newEncryptionSalt)
                    ));

                    request.MasterPasswordKey = newMasterPasswordKey;
                    request.AuthSalt          = newAuthSalt;
                    request.EncryptionSalt    = newEncryptionSalt;

                    // Re-encrypt all vault items with the new key on a background thread.
                    string oldVaultKey = SessionHelper.SessionVaultKey;
                    // Deep-copy only the fields read during re-encryption. The snapshot is taken
                    // here on the UI thread; PasswordMonitorService never structurally modifies
                    // CachedVault (it only makes server calls), so this enumeration is safe.
                    var vaultSnapshot = SessionHelper.CachedVault
                        .Select(v => new VaultItem { Id = v.Id, IV = v.IV, Tag = v.Tag, CipherText = v.CipherText })
                        .ToList();
                    var reEncryptedItems = await Task.Run(() =>
                    {
                        var items = new List<VaultItem>();
                        foreach (var item in vaultSnapshot)
                        {
                            // Decrypt with old key
                            string plaintext = EncryptionHelper.DecryptAesGcm(
                                item.IV, item.Tag, item.CipherText, oldVaultKey);

                            // Re-encrypt with new key
                            var (newIV, newTag, newCipherText) = EncryptionHelper.EncryptAesGcm(plaintext, newVaultKey);

                            items.Add(new VaultItem
                            {
                                Id         = item.Id,
                                IV         = newIV,
                                Tag        = newTag,
                                CipherText = newCipherText
                            });
                        }
                        return items;
                    });

                    request.VaultItems = reEncryptedItems;

                    // Send the update to the server.
                    var profileService = new ProfileService();
                    var result = await profileService.UpdateAccountAsync(request);

                    if (result.Success)
                    {
                        // Update the session with the new vault key.
                        SessionHelper.StartSession(newVaultKey);
                        await StorageHelper.SaveVaultKeyAsync(newVaultKey);
                        await StorageHelper.SaveUsernameAsync(username);

                        // Update the cached vault items with the new encryption.
                        for (int i = 0; i < SessionHelper.CachedVault.Count; i++)
                        {
                            var cached = SessionHelper.CachedVault[i];
                            var reenc  = reEncryptedItems.Find(r => r.Id == cached.Id);
                            if (reenc != null)
                            {
                                cached.IV         = reenc.IV;
                                cached.Tag        = reenc.Tag;
                                cached.CipherText = reenc.CipherText;
                            }
                        }

                        SessionHelper.InvalidateWarnings();

                        Toast.MakeText(this, Resource.String.edit_account_success, ToastLength.Short).Show();
                        SetResult(Result.Ok, new Intent().PutExtra(ResultUpdated, true));
                        Finish();
                    }
                    else
                    {
                        FormUiHelper.ShowError(textViewGeneralError, result.Message);
                    }
                }
                else
                {
                    // Username/email only change — no re-encryption needed.
                    var profileService = new ProfileService();
                    var result = await profileService.UpdateAccountAsync(request);

                    if (result.Success)
                    {
                        await StorageHelper.SaveUsernameAsync(username);

                        Toast.MakeText(this, Resource.String.edit_account_success, ToastLength.Short).Show();
                        SetResult(Result.Ok, new Intent().PutExtra(ResultUpdated, true));
                        Finish();
                    }
                    else
                    {
                        FormUiHelper.ShowError(textViewGeneralError, result.Message);
                    }
                }
            }
            catch (Exception)
            {
                FormUiHelper.ShowError(textViewGeneralError, GetString(Resource.String.edit_account_error));
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>Validates all edit account form fields and shows errors for any invalid values.</summary>
        private bool ValidateInputs(string username, string email,
            string currentPassword, string newPassword, string confirmPassword,
            bool isChangingPassword)
        {
            bool isValid = true;

            var usernameResult = ValidationHelper.ValidateUsername(username);
            if (!usernameResult.IsValid) { FormUiHelper.ShowError(textViewUsernameError, usernameResult.ErrorMessage); isValid = false; }

            var emailResult = ValidationHelper.ValidateEmail(email);
            if (!emailResult.IsValid) { FormUiHelper.ShowError(textViewEmailError, emailResult.ErrorMessage); isValid = false; }

            if (isChangingPassword)
            {
                if (string.IsNullOrEmpty(currentPassword))
                {
                    FormUiHelper.ShowError(textViewCurrentPasswordError, GetString(Resource.String.edit_account_current_password_required));
                    isValid = false;
                }

                var passwordResult = ValidationHelper.ValidatePassword(newPassword);
                if (!passwordResult.IsValid) { FormUiHelper.ShowError(textViewNewPasswordError, passwordResult.ErrorMessage); isValid = false; }

                var confirmResult = ValidationHelper.ValidatePasswordsMatch(newPassword, confirmPassword);
                if (!confirmResult.IsValid) { FormUiHelper.ShowError(textViewConfirmNewPasswordError, confirmResult.ErrorMessage); isValid = false; }
            }

            return isValid;
        }

        // -- UI helpers delegating to the shared FormUiHelper (DRY) --

        /// <summary>Hides all field-level and general error messages.</summary>
        private void ClearErrors()
        {
            FormUiHelper.HideError(textViewUsernameError);
            FormUiHelper.HideError(textViewEmailError);
            FormUiHelper.HideError(textViewCurrentPasswordError);
            FormUiHelper.HideError(textViewNewPasswordError);
            FormUiHelper.HideError(textViewConfirmNewPasswordError);
            FormUiHelper.HideError(textViewGeneralError);
        }

        /// <summary>Toggles the loading state, showing the progress bar and disabling form controls.</summary>
        private void SetLoadingState(bool isLoading)
        {
            progressBarEditAccount.Visibility = isLoading ? ViewStates.Visible : ViewStates.Gone;
            buttonSave.Enabled               = !isLoading;
            buttonGeneratePassword.Enabled   = !isLoading;
            editTextUsername.Enabled          = !isLoading;
            editTextEmail.Enabled             = !isLoading;
            editTextCurrentPassword.Enabled   = !isLoading;
            editTextNewPassword.Enabled       = !isLoading;
            editTextConfirmNewPassword.Enabled = !isLoading;
        }
    }
}
