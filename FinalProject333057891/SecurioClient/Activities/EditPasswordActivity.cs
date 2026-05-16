using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Google.Android.Material.TextField;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurioClient.Activities
{
    /// <summary>Activity for editing an existing password entry in the vault using the shared activity_entry.xml layout.</summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class EditPasswordActivity : BasePasswordEntryActivity
    {
        // Intent extras used to pass the item data into this activity.
        public const string ExtraEntryId = "EXTRA_ENTRY_ID";
        public const string ExtraSiteName = "EXTRA_SITE_NAME";
        public const string ExtraUsername = "EXTRA_USERNAME";
        public const string ExtraNotes = "EXTRA_NOTES";
        // Existing encrypted fields — passed in so the user does not have to re-enter the password.
        public const string ExtraIV = "EXTRA_IV";
        public const string ExtraTag = "EXTRA_TAG";
        public const string ExtraCipherText = "EXTRA_CIPHER_TEXT";
        public const string ExtraSha1Hash = "EXTRA_SHA1_HASH";
        public const string ExtraIsLeaked = "EXTRA_IS_LEAKED";

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

        public const int RequestCodeEdit = 1002;

        // -- Edit-mode-specific views ----------------------------
        private TextInputLayout textInputLayoutPassword;
        private TextInputLayout textInputLayoutConfirmPassword;

        // The ID of the vault item being edited.
        private int entryId;

        // O(1) duplicate-check set built from the live vault cache in PopulateExistingEntries.
        private HashSet<string> _existingEntryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Placeholder shown in the password field to indicate an existing password is stored.
        // Cleared when the user focuses the field; restored if they leave without typing.
        private const string PasswordPlaceholder = "••••••••";
        private bool _passwordIsPlaceholder;

        // -- Lifecycle ------------------------------------------

        /// <summary>Initializes the activity, inflates the layout, and sets up views and event handlers.</summary>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_entry);

            InitializeViews();
            ConfigureForEditMode();
            PopulateExistingEntries();
            PopulateFieldsFromIntent();
            SetupEventHandlers();
            ConfigurePasswordPlaceholder();
        }

        // -- Setup helpers --------------------------------------

        /// <summary>Finds and assigns all view references from the layout, including edit-mode-specific inputs.</summary>
        protected override void InitializeViews()
        {
            base.InitializeViews();
            textInputLayoutPassword = FindViewById<TextInputLayout>(Resource.Id.textInputLayoutEntryPassword);
            textInputLayoutConfirmPassword = FindViewById<TextInputLayout>(Resource.Id.textInputLayoutEntryConfirmPassword);
        }

        /// <summary>Sets the title, subtitle, button label, and hint text for edit mode.</summary>
        private void ConfigureForEditMode()
        {
            TextViewTitle.Text = GetString(Resource.String.entry_title_edit);
            TextViewSubtitle.Text = GetString(Resource.String.entry_subtitle_edit);
            ButtonSave.Text = GetString(Resource.String.entry_button_update);

            // Update hints to make clear that the password field is optional in edit mode.
            textInputLayoutPassword.Hint = GetString(Resource.String.entry_password_edit_hint);
            textInputLayoutConfirmPassword.Hint = GetString(Resource.String.entry_confirm_password_edit_hint);
        }

        /// <summary>Builds the duplicate-check set from the live vault cache, excluding the item being edited, for O(1) lookups.</summary>
        private void PopulateExistingEntries()
        {
            entryId = Intent.GetIntExtra(ExtraEntryId, 0);
            _existingEntryKeys = new HashSet<string>(
                (SessionHelper.CachedVault ?? new List<VaultItem>())
                    .Where(e => e.Id != entryId)
                    .Select(e => $"{e.AccountName}\0{e.AccountUsername}"),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Pre-fills the form fields with the data passed via the launching Intent.</summary>
        private void PopulateFieldsFromIntent()
        {
            EditTextSiteName.Text = Intent.GetStringExtra(ExtraSiteName) ?? string.Empty;
            EditTextUsername.Text = Intent.GetStringExtra(ExtraUsername) ?? string.Empty;
            EditTextNotes.Text = Intent.GetStringExtra(ExtraNotes) ?? string.Empty;
            // Password and confirm password are intentionally left empty.
            // Existing encrypted data is carried via ExtraIV/ExtraTag/ExtraCipherText/ExtraSha1Hash.
        }

        /// <summary>Wires up click and text-change event handlers for the entry form controls.</summary>
        private void SetupEventHandlers()
        {
            ImageViewBack.Click += (s, e) =>
            {
                SetResult(Result.Canceled);
                Finish();
            };

            ButtonGeneratePassword.Click += (s, e) =>
            {
                _passwordIsPlaceholder = false;
                string generated = ValidationHelper.GenerateStrongPassword();
                EditTextPassword.Text = generated;
                EditTextConfirmPassword.Text = generated;
            };

            ButtonSave.Click += async (s, e) => await OnUpdateClicked();

            // Real-time validation: site name
            EditTextSiteName.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                string site = EditTextSiteName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(site))
                    FormUiHelper.ShowError(TextViewSiteNameError, GetString(Resource.String.entry_error_site_required));
                else
                    FormUiHelper.HideError(TextViewSiteNameError);

                FormUiHelper.HideError(TextViewGeneralError);
            }));

            // Real-time validation: username
            EditTextUsername.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                string username = EditTextUsername.Text?.Trim();
                if (string.IsNullOrWhiteSpace(username))
                    FormUiHelper.ShowError(TextViewUsernameError, GetString(Resource.String.entry_error_username_required));
                else
                    FormUiHelper.HideError(TextViewUsernameError);

                FormUiHelper.HideError(TextViewGeneralError);
            }));

            // Real-time validation: password strength (informational only, never blocks)
            EditTextPassword.AddTextChangedListener(new SimpleTextWatcher(text =>
            {
                if (_passwordIsPlaceholder) return;
                UpdatePasswordStrengthIndicator(text);

                // Re-validate confirm password match whenever the password field changes.
                if (!string.IsNullOrEmpty(EditTextConfirmPassword.Text))
                    ValidatePasswordsMatch();
            }));

            // Real-time validation: confirm password
            EditTextConfirmPassword.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                ValidatePasswordsMatch();
            }));
        }

        /// <summary>Seeds the password field with a masked placeholder and clears it on focus so the user can type a new value.</summary>
        private void ConfigurePasswordPlaceholder()
        {
            _passwordIsPlaceholder = true;
            EditTextPassword.Text = PasswordPlaceholder;

            EditTextPassword.FocusChange += (s, e) =>
            {
                if (e.HasFocus && _passwordIsPlaceholder)
                {
                    // User tapped the field — clear the placeholder so they can type.
                    _passwordIsPlaceholder = false;
                    EditTextPassword.Text = string.Empty;
                }
                else if (!e.HasFocus && string.IsNullOrEmpty(EditTextPassword.Text))
                {
                    // User left the field without entering anything — restore the placeholder.
                    _passwordIsPlaceholder = true;
                    EditTextPassword.Text = PasswordPlaceholder;
                }
            };
        }

        // -- Update logic ---------------------------------------

        /// <summary>Validates inputs, optionally re-encrypts the password, and submits the vault entry update.</summary>
        private async Task OnUpdateClicked()
        {
            ClearErrors();

            string siteName = EditTextSiteName.Text?.Trim();
            string username = EditTextUsername.Text?.Trim();
            // Treat the placeholder as an empty field (user kept the existing password).
            string password = _passwordIsPlaceholder ? string.Empty : EditTextPassword.Text;
            string confirmPassword = _passwordIsPlaceholder ? string.Empty : EditTextConfirmPassword.Text;
            string notes = EditTextNotes.Text?.Trim();

            if (!ValidateInputs(siteName, username, password, confirmPassword))
                return;

            if (IsDuplicate(siteName, username))
            {
                FormUiHelper.ShowError(TextViewGeneralError, GetString(Resource.String.entry_error_duplicate));
                return;
            }

            SetLoadingState(true);

            try
            {
                string iv, tag, cipherText, sha1Hash;
                bool isLeaked;

                if (!string.IsNullOrEmpty(password))
                {
                    // User entered a new password — encrypt it and check HIBP.
                    // AES-GCM encryption runs off the UI thread to prevent ANR.
                    string vaultKey = SessionHelper.SessionVaultKey;
                    (iv, tag, cipherText) = await Task.Run(() => EncryptionHelper.EncryptAesGcm(password, vaultKey));
                    sha1Hash = EncryptionHelper.ComputeSha1Hash(password);
                    isLeaked = await HibpClientService.IsPasswordPwnedAsync(sha1Hash);
                }
                else
                {
                    // Password left blank — keep the existing encrypted data.
                    iv         = Intent.GetStringExtra(ExtraIV) ?? string.Empty;
                    tag        = Intent.GetStringExtra(ExtraTag) ?? string.Empty;
                    cipherText = Intent.GetStringExtra(ExtraCipherText) ?? string.Empty;
                    sha1Hash   = Intent.GetStringExtra(ExtraSha1Hash) ?? string.Empty;
                    isLeaked   = Intent.GetBooleanExtra(ExtraIsLeaked, false);
                }

                var vaultItem = new VaultItem
                {
                    Id              = entryId,
                    AccountName     = siteName,
                    AccountUsername  = username,
                    IV              = iv,
                    Tag             = tag,
                    CipherText      = cipherText,
                    Notes           = notes ?? string.Empty,
                    Sha1Hash        = sha1Hash,
                    IsLeaked        = isLeaked
                };

                var updateRequest = new UpdateVaultItemRequest
                {
                    Item            = vaultItem,
                    PasswordChanged = !string.IsNullOrEmpty(password)
                };

                var vaultService = new VaultService();
                var result = await vaultService.UpdateVaultItemAsync(updateRequest);

                if (result.Success)
                {
                    // Return the updated data to VaultActivity so it can refresh its local list.
                    var resultIntent = new Intent();
                    resultIntent.PutExtra(ResultEntryId, entryId);
                    resultIntent.PutExtra(ResultSiteName, siteName);
                    resultIntent.PutExtra(ResultUsername, username);
                    resultIntent.PutExtra(ResultNotes, notes ?? string.Empty);
                    resultIntent.PutExtra(ResultIV, iv);
                    resultIntent.PutExtra(ResultTag, tag);
                    resultIntent.PutExtra(ResultCipherText, cipherText);
                    resultIntent.PutExtra(ResultSha1Hash, sha1Hash);
                    resultIntent.PutExtra(ResultIsLeaked, result.Data?.IsLeaked ?? isLeaked);
                    resultIntent.PutExtra(ResultLastUpdate, result.Data?.LastUpdate.Ticks ?? 0L);

                    SetResult(Result.Ok, resultIntent);

                    Toast.MakeText(this,
                        GetString(Resource.String.entry_updated_success),
                        ToastLength.Short).Show();

                    Finish();
                }
                else
                {
                    FormUiHelper.ShowError(TextViewGeneralError, result.Message);
                }
            }
            catch (Exception)
            {
                FormUiHelper.ShowError(TextViewGeneralError, GetString(Resource.String.entry_error_update_failed));
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
                FormUiHelper.ShowError(TextViewSiteNameError, GetString(Resource.String.entry_error_site_required));
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                FormUiHelper.ShowError(TextViewUsernameError, GetString(Resource.String.entry_error_username_required));
                valid = false;
            }

            // In edit mode the password is optional; only validate when provided.
            if (!string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(confirmPassword))
            {
                // If one is set, both must be set and equal.
                if (string.IsNullOrEmpty(password))
                {
                    FormUiHelper.ShowError(TextViewPasswordError, GetString(Resource.String.entry_error_password_required));
                    valid = false;
                }
                else if (string.IsNullOrEmpty(confirmPassword))
                {
                    FormUiHelper.ShowError(TextViewConfirmPasswordError, GetString(Resource.String.entry_error_confirm_password_required));
                    valid = false;
                }
                else if (password != confirmPassword)
                {
                    FormUiHelper.ShowError(TextViewConfirmPasswordError, GetString(Resource.String.entry_error_passwords_mismatch));
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>Returns true when an entry with the same site name and username already exists in the vault (excluding the item being edited).</summary>
        private bool IsDuplicate(string siteName, string username)
        {
            return _existingEntryKeys.Contains($"{siteName}\0{username}");
        }
    }
}
