using Android.App;
using Android.OS;
using Android.Widget;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurioClient.Activities
{
    /// <summary>Activity for adding a new password entry to the vault using the shared activity_entry.xml layout.</summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AddPasswordActivity : BasePasswordEntryActivity
    {
        // Result extras returned to the caller.
        public const string ResultEntryId = "RESULT_ENTRY_ID";
        public const string ResultSiteName = "RESULT_SITE_NAME";
        public const string ResultUsername = "RESULT_USERNAME";
        public const string ResultNotes = "RESULT_NOTES";
        public const string ResultIV = "RESULT_IV";
        public const string ResultTag = "RESULT_TAG";
        public const string ResultCipherText = "RESULT_CIPHER_TEXT";
        public const string ResultSha1Hash = "RESULT_SHA1_HASH";
        public const string ResultIsLeaked = "RESULT_IS_LEAKED";
        public const string ResultLastUpdate = "RESULT_LAST_UPDATE";

        public const int RequestCodeAdd = 1001;

        // O(1) duplicate-check set built from the live vault cache in PopulateExistingEntries.
        private HashSet<string> _existingEntryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // -- Lifecycle ------------------------------------------

        /// <summary>Initializes the activity, inflates the layout, and sets up views and event handlers.</summary>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_entry);

            InitializeViews();
            ConfigureForAddMode();
            PopulateExistingEntries();
            SetupEventHandlers();
        }

        // -- Setup helpers --------------------------------------

        /// <summary>Sets the title, subtitle, and button label for add mode.</summary>
        private void ConfigureForAddMode()
        {
            textViewTitle.Text = GetString(Resource.String.entry_title_add);
            textViewSubtitle.Text = GetString(Resource.String.entry_subtitle_add);
            buttonSave.Text = GetString(Resource.String.entry_button_save);
        }

        /// <summary>Builds the duplicate-check set from the live vault cache for O(1) lookups.</summary>
        private void PopulateExistingEntries()
        {
            _existingEntryKeys = new HashSet<string>(
                (SessionHelper.CachedVault ?? new List<VaultItem>())
                    .Select(e => $"{e.AccountName}\0{e.AccountUsername}"),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Wires up click and text-change event handlers for the entry form controls.</summary>
        private void SetupEventHandlers()
        {
            imageViewBack.Click += (s, e) =>
            {
                SetResult(Result.Canceled);
                Finish();
            };

            buttonGeneratePassword.Click += (s, e) =>
            {
                editTextPassword.Text = ValidationHelper.GenerateStrongPassword();
            };

            buttonSave.Click += async (s, e) => await OnSaveClicked();

            // Real-time validation: site name
            editTextSiteName.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                string site = editTextSiteName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(site))
                    FormUiHelper.ShowError(textViewSiteNameError, GetString(Resource.String.entry_error_site_required));
                else
                    FormUiHelper.HideError(textViewSiteNameError);

                // Clear any previous duplicate error when the user changes the field.
                FormUiHelper.HideError(textViewGeneralError);
            }));

            // Real-time validation: username
            editTextUsername.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                string username = editTextUsername.Text?.Trim();
                if (string.IsNullOrWhiteSpace(username))
                    FormUiHelper.ShowError(textViewUsernameError, GetString(Resource.String.entry_error_username_required));
                else
                    FormUiHelper.HideError(textViewUsernameError);

                FormUiHelper.HideError(textViewGeneralError);
            }));

            // Real-time validation: password (strength bar — informational only, never blocks)
            editTextPassword.AddTextChangedListener(new SimpleTextWatcher(text =>
            {
                UpdatePasswordStrengthIndicator(text);

                // Re-validate confirm password if the user has already typed in it.
                if (!string.IsNullOrEmpty(editTextConfirmPassword.Text))
                    ValidatePasswordsMatch();
            }));

            // Real-time validation: confirm password
            editTextConfirmPassword.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                ValidatePasswordsMatch();
            }));
        }

        // -- Save logic -----------------------------------------

        /// <summary>Validates inputs, encrypts the password, checks for breaches, and saves the new vault entry.</summary>
        private async Task OnSaveClicked()
        {
            ClearErrors();

            string siteName = editTextSiteName.Text?.Trim();
            string username = editTextUsername.Text?.Trim();
            string password = editTextPassword.Text;
            string confirmPassword = editTextConfirmPassword.Text;
            string notes = editTextNotes.Text?.Trim();

            if (!ValidateInputs(siteName, username, password, confirmPassword))
                return;

            if (IsDuplicate(siteName, username))
            {
                FormUiHelper.ShowError(textViewGeneralError, GetString(Resource.String.entry_error_duplicate));
                return;
            }

            SetLoadingState(true);

            try
            {
                // Encrypt the password with AES-GCM using the session vault key, and compute
                // the SHA-1 hash for HIBP checks — both run off the UI thread.
                string vaultKey = SessionHelper.SessionVaultKey;
                var (iv, tag, cipherText) = await Task.Run(() => EncryptionHelper.EncryptAesGcm(password, vaultKey));
                string sha1Hash = EncryptionHelper.ComputeSha1Hash(password);
                bool isLeaked = await HibpClientService.IsPasswordPwnedAsync(sha1Hash);

                var vaultItem = new VaultItem
                {
                    AccountName = siteName,
                    AccountUsername = username,
                    IV = iv,
                    Tag = tag,
                    CipherText = cipherText,
                    Notes = notes ?? string.Empty,
                    Sha1Hash = sha1Hash,
                    IsLeaked = isLeaked
                };

                var vaultService = new VaultService();
                var result = await vaultService.AddVaultItemAsync(vaultItem);

                if (result.Success)
                {
                    // Return the data to VaultActivity so it can update its local list.
                    var resultIntent = new Android.Content.Intent();
                    resultIntent.PutExtra(ResultEntryId, result.Data?.Id ?? 0);
                    resultIntent.PutExtra(ResultSiteName, siteName);
                    resultIntent.PutExtra(ResultUsername, username);
                    resultIntent.PutExtra(ResultNotes, notes ?? string.Empty);
                    resultIntent.PutExtra(ResultIV, iv);
                    resultIntent.PutExtra(ResultTag, tag);
                    resultIntent.PutExtra(ResultCipherText, cipherText);
                    resultIntent.PutExtra(ResultSha1Hash, sha1Hash);
                    resultIntent.PutExtra(ResultIsLeaked, result.Data?.IsLeaked ?? isLeaked);
                    resultIntent.PutExtra(ResultLastUpdate, result.Data?.LastUpdate.Ticks ?? DateTime.UtcNow.Ticks);

                    SetResult(Result.Ok, resultIntent);

                    Toast.MakeText(this,
                        GetString(Resource.String.entry_saved_success),
                        ToastLength.Short).Show();

                    Finish();
                }
                else
                {
                    FormUiHelper.ShowError(textViewGeneralError, result.Message);
                }
            }
            catch (Exception)
            {
                FormUiHelper.ShowError(textViewGeneralError, GetString(Resource.String.entry_error_save_failed));
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        // -- Validation -----------------------------------------

        /// <summary>Validates all entry form fields and shows errors for any invalid values.</summary>
        private bool ValidateInputs(string siteName, string username, string password, string confirmPassword)
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(siteName))
            {
                FormUiHelper.ShowError(textViewSiteNameError, GetString(Resource.String.entry_error_site_required));
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                FormUiHelper.ShowError(textViewUsernameError, GetString(Resource.String.entry_error_username_required));
                valid = false;
            }

            if (string.IsNullOrEmpty(password))
            {
                FormUiHelper.ShowError(textViewPasswordError, GetString(Resource.String.entry_error_password_required));
                valid = false;
            }

            if (string.IsNullOrEmpty(confirmPassword))
            {
                FormUiHelper.ShowError(textViewConfirmPasswordError, GetString(Resource.String.entry_error_confirm_password_required));
                valid = false;
            }
            else if (!string.IsNullOrEmpty(password) && password != confirmPassword)
            {
                FormUiHelper.ShowError(textViewConfirmPasswordError, GetString(Resource.String.entry_error_passwords_mismatch));
                valid = false;
            }

            // NOTE: Weak passwords are intentionally allowed because the user cannot
            // control what password policy external sites enforce (e.g. a 4-digit PIN).

            return valid;
        }

        /// <summary>Returns true when an entry with the same site name and username already exists in the vault.</summary>
        private bool IsDuplicate(string siteName, string username)
        {
            return _existingEntryKeys.Contains($"{siteName}\0{username}");
        }
    }
}
