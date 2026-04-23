using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.Button;
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
    /// <summary>
    /// Activity for editing an existing password entry in the vault.
    /// Reuses the shared <c>activity_entry.xml</c> layout with title/button
    /// configured for edit mode.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class EditPasswordActivity : AppCompatActivity
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

        // ── Views ──────────────────────────────────────────────
        private ImageView imageViewBack;
        private TextView textViewTitle;
        private TextView textViewSubtitle;

        private TextInputLayout textInputLayoutPassword;
        private TextInputLayout textInputLayoutConfirmPassword;

        private TextInputEditText editTextSiteName;
        private TextInputEditText editTextUsername;
        private TextInputEditText editTextPassword;
        private TextInputEditText editTextConfirmPassword;
        private TextInputEditText editTextNotes;

        private TextView textViewSiteNameError;
        private TextView textViewUsernameError;
        private TextView textViewPasswordError;
        private TextView textViewConfirmPasswordError;
        private TextView textViewGeneralError;

        private ProgressBar progressBarStrength;
        private TextView textViewStrengthHint;

        private MaterialButton buttonGeneratePassword;
        private MaterialButton buttonSave;
        private ProgressBar progressBar;

        // The ID of the vault item being edited.
        private int entryId;

        // Holds existing entries for duplicate checking (excludes the item being edited).
        private List<VaultItem> existingEntries = new List<VaultItem>();

        // Placeholder shown in the password field to indicate an existing password is stored.
        // Cleared when the user focuses the field; restored if they leave without typing.
        private const string PasswordPlaceholder = "••••••••";
        private bool _passwordIsPlaceholder;

        // ── Lifecycle ──────────────────────────────────────────

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

        // ── Setup helpers ──────────────────────────────────────

        private void InitializeViews()
        {
            imageViewBack = FindViewById<ImageView>(Resource.Id.imageViewEntryBack);
            textViewTitle = FindViewById<TextView>(Resource.Id.textViewEntryTitle);
            textViewSubtitle = FindViewById<TextView>(Resource.Id.textViewEntrySubtitle);

            textInputLayoutPassword = FindViewById<TextInputLayout>(Resource.Id.textInputLayoutEntryPassword);
            textInputLayoutConfirmPassword = FindViewById<TextInputLayout>(Resource.Id.textInputLayoutEntryConfirmPassword);

            editTextSiteName = FindViewById<TextInputEditText>(Resource.Id.editTextEntrySiteName);
            editTextUsername = FindViewById<TextInputEditText>(Resource.Id.editTextEntryUsername);
            editTextPassword = FindViewById<TextInputEditText>(Resource.Id.editTextEntryPassword);
            editTextConfirmPassword = FindViewById<TextInputEditText>(Resource.Id.editTextEntryConfirmPassword);
            editTextNotes = FindViewById<TextInputEditText>(Resource.Id.editTextEntryNotes);

            textViewSiteNameError = FindViewById<TextView>(Resource.Id.textViewEntrySiteNameError);
            textViewUsernameError = FindViewById<TextView>(Resource.Id.textViewEntryUsernameError);
            textViewPasswordError = FindViewById<TextView>(Resource.Id.textViewEntryPasswordError);
            textViewConfirmPasswordError = FindViewById<TextView>(Resource.Id.textViewEntryConfirmPasswordError);
            textViewGeneralError = FindViewById<TextView>(Resource.Id.textViewEntryGeneralError);

            progressBarStrength = FindViewById<ProgressBar>(Resource.Id.progressBarEntryStrength);
            textViewStrengthHint = FindViewById<TextView>(Resource.Id.textViewEntryStrengthHint);

            buttonGeneratePassword = FindViewById<MaterialButton>(Resource.Id.buttonEntryGeneratePassword);
            buttonSave = FindViewById<MaterialButton>(Resource.Id.buttonEntrySave);
            progressBar = FindViewById<ProgressBar>(Resource.Id.progressBarEntry);
        }

        private void ConfigureForEditMode()
        {
            textViewTitle.Text = GetString(Resource.String.entry_title_edit);
            textViewSubtitle.Text = GetString(Resource.String.entry_subtitle_edit);
            buttonSave.Text = GetString(Resource.String.entry_button_update);

            // Update hints to make clear that the password field is optional in edit mode.
            textInputLayoutPassword.Hint = GetString(Resource.String.entry_password_edit_hint);
            textInputLayoutConfirmPassword.Hint = GetString(Resource.String.entry_confirm_password_edit_hint);
        }

        /// <summary>
        /// Loads the existing vault entries (excluding the current item) for duplicate checking.
        /// </summary>
        private void PopulateExistingEntries()
        {
            entryId = Intent.GetIntExtra(ExtraEntryId, 0);
            existingEntries = (VaultEntryCache.Entries ?? new List<VaultItem>())
                .Where(e => e.Id != entryId)
                .ToList();
        }

        /// <summary>
        /// Pre-fills the form fields with the data passed via the launching Intent.
        /// The password fields are left blank — the user may enter a new password or leave
        /// them empty to keep the existing encrypted data unchanged.
        /// </summary>
        private void PopulateFieldsFromIntent()
        {
            editTextSiteName.Text = Intent.GetStringExtra(ExtraSiteName) ?? string.Empty;
            editTextUsername.Text = Intent.GetStringExtra(ExtraUsername) ?? string.Empty;
            editTextNotes.Text = Intent.GetStringExtra(ExtraNotes) ?? string.Empty;
            // Password and confirm password are intentionally left empty.
            // Existing encrypted data is carried via ExtraIV/ExtraTag/ExtraCipherText/ExtraSha1Hash.
        }

        private void SetupEventHandlers()
        {
            imageViewBack.Click += (s, e) =>
            {
                SetResult(Result.Canceled);
                Finish();
            };

            buttonGeneratePassword.Click += (s, e) =>
            {
                _passwordIsPlaceholder = false;
                string generated = ValidationHelper.GenerateStrongPassword();
                editTextPassword.Text = generated;
                editTextConfirmPassword.Text = generated;
            };

            buttonSave.Click += async (s, e) => await OnUpdateClicked();

            // Real-time validation: site name
            editTextSiteName.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                string site = editTextSiteName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(site))
                    ShowError(textViewSiteNameError, GetString(Resource.String.entry_error_site_required));
                else
                    HideError(textViewSiteNameError);

                HideError(textViewGeneralError);
            }));

            // Real-time validation: username
            editTextUsername.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                string username = editTextUsername.Text?.Trim();
                if (string.IsNullOrWhiteSpace(username))
                    ShowError(textViewUsernameError, GetString(Resource.String.entry_error_username_required));
                else
                    HideError(textViewUsernameError);

                HideError(textViewGeneralError);
            }));

            // Real-time validation: password strength (informational only, never blocks)
            editTextPassword.AddTextChangedListener(new SimpleTextWatcher(text =>
            {
                if (_passwordIsPlaceholder) return;
                UpdatePasswordStrengthIndicator(text);

                // Re-validate confirm password match whenever the password field changes.
                if (!string.IsNullOrEmpty(editTextConfirmPassword.Text))
                    ValidatePasswordsMatch();
            }));

            // Real-time validation: confirm password
            editTextConfirmPassword.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                ValidatePasswordsMatch();
            }));
        }

        /// <summary>
        /// Seeds the password field with a masked placeholder to indicate an existing password
        /// is stored. Clears on focus so the user can type a new value, and restores the
        /// placeholder if they leave the field without typing anything.
        /// </summary>
        private void ConfigurePasswordPlaceholder()
        {
            _passwordIsPlaceholder = true;
            editTextPassword.Text = PasswordPlaceholder;

            editTextPassword.FocusChange += (s, e) =>
            {
                if (e.HasFocus && _passwordIsPlaceholder)
                {
                    // User tapped the field — clear the placeholder so they can type.
                    _passwordIsPlaceholder = false;
                    editTextPassword.Text = string.Empty;
                }
                else if (!e.HasFocus && string.IsNullOrEmpty(editTextPassword.Text))
                {
                    // User left the field without entering anything — restore the placeholder.
                    _passwordIsPlaceholder = true;
                    editTextPassword.Text = PasswordPlaceholder;
                }
            };
        }

        // ── Update logic ───────────────────────────────────────

        private async Task OnUpdateClicked()
        {
            ClearErrors();

            string siteName = editTextSiteName.Text?.Trim();
            string username = editTextUsername.Text?.Trim();
            // Treat the placeholder as an empty field (user kept the existing password).
            string password = _passwordIsPlaceholder ? string.Empty : editTextPassword.Text;
            string confirmPassword = _passwordIsPlaceholder ? string.Empty : editTextConfirmPassword.Text;
            string notes = editTextNotes.Text?.Trim();

            if (!ValidateInputs(siteName, username, password, confirmPassword))
                return;

            if (IsDuplicate(siteName, username))
            {
                ShowError(textViewGeneralError, GetString(Resource.String.entry_error_duplicate));
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
                    string vaultKey = SessionHelper.SessionVaultKey;
                    (iv, tag, cipherText) = EncryptionHelper.EncryptAesGcm(password, vaultKey);
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
                    IsLeaked        = isLeaked,
                    PasswordChanged = !string.IsNullOrEmpty(password)
                };

                var vaultService = new VaultService();
                var result = await vaultService.UpdateVaultItemAsync(vaultItem);

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
                    ShowError(textViewGeneralError, result.Message);
                }
            }
            catch (Exception ex)
            {
                ShowError(textViewGeneralError, GetString(Resource.String.entry_error_update_failed));
                System.Diagnostics.Debug.WriteLine($"[EDIT PASSWORD ERROR] {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        // ── Validation ─────────────────────────────────────────

        private bool ValidateInputs(string siteName, string username, string password, string confirmPassword)
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(siteName))
            {
                ShowError(textViewSiteNameError, GetString(Resource.String.entry_error_site_required));
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError(textViewUsernameError, GetString(Resource.String.entry_error_username_required));
                valid = false;
            }

            // In edit mode the password is optional; only validate when provided.
            if (!string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(confirmPassword))
            {
                // If one is set, both must be set and equal.
                if (string.IsNullOrEmpty(password))
                {
                    ShowError(textViewPasswordError, GetString(Resource.String.entry_error_password_required));
                    valid = false;
                }
                else if (string.IsNullOrEmpty(confirmPassword))
                {
                    ShowError(textViewConfirmPasswordError, GetString(Resource.String.entry_error_confirm_password_required));
                    valid = false;
                }
                else if (password != confirmPassword)
                {
                    ShowError(textViewConfirmPasswordError, GetString(Resource.String.entry_error_passwords_mismatch));
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>
        /// Returns <c>true</c> when an entry with the same site name AND username
        /// already exists in the vault (excluding the item currently being edited).
        /// </summary>
        private bool IsDuplicate(string siteName, string username)
        {
            return existingEntries.Any(e =>
                string.Equals(e.AccountName, siteName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.AccountUsername, username, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Shows or hides the confirm password mismatch error during real-time validation.
        /// Only fires when the confirm field is non-empty.
        /// </summary>
        private void ValidatePasswordsMatch()
        {
            string password = editTextPassword.Text;
            string confirm = editTextConfirmPassword.Text;
            if (string.IsNullOrEmpty(confirm))
            {
                HideError(textViewConfirmPasswordError);
                return;
            }
            if (password != confirm)
                ShowError(textViewConfirmPasswordError, GetString(Resource.String.entry_error_passwords_mismatch));
            else
                HideError(textViewConfirmPasswordError);
        }

        // ── Password strength indicator (informational) ────────

        private void UpdatePasswordStrengthIndicator(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                progressBarStrength.Visibility = ViewStates.Gone;
                textViewStrengthHint.Visibility = ViewStates.Gone;
                HideError(textViewPasswordError);
                return;
            }

            int score = ValidationHelper.GetPasswordScore(password);
            progressBarStrength.Visibility = ViewStates.Visible;
            progressBarStrength.Progress = score;
            progressBarStrength.ProgressTintList =
                ColorStateList.ValueOf(new Android.Graphics.Color(Resources.GetColor(ScoreToColorRes(score))));

            string hint = ValidationHelper.GetMissingCriteriaHint(password);
            textViewStrengthHint.Text = hint;
            textViewStrengthHint.Visibility = ViewStates.Visible;
            textViewStrengthHint.SetTextColor(new Android.Graphics.Color(
                Resources.GetColor(score == 5
                    ? Resource.Color.passwordStrengthVeryStrong
                    : Resource.Color.signupHintText)));
        }

        private static int ScoreToColorRes(int score)
        {
            switch (score)
            {
                case 1: return Resource.Color.passwordStrengthWeak;
                case 2: return Resource.Color.passwordStrengthPoor;
                case 3: return Resource.Color.passwordStrengthFair;
                case 4: return Resource.Color.passwordStrengthStrong;
                default: return Resource.Color.passwordStrengthVeryStrong;
            }
        }

        // ── UI helpers ─────────────────────────────────────────

        private void ShowError(TextView errorView, string message)
        {
            errorView.Text = message;
            errorView.Visibility = ViewStates.Visible;
        }

        private void HideError(TextView errorView)
        {
            errorView.Text = null;
            errorView.Visibility = ViewStates.Gone;
        }

        private void ClearErrors()
        {
            HideError(textViewSiteNameError);
            HideError(textViewUsernameError);
            HideError(textViewPasswordError);
            HideError(textViewConfirmPasswordError);
            HideError(textViewGeneralError);
        }

        private void SetLoadingState(bool isLoading)
        {
            progressBar.Visibility = isLoading ? ViewStates.Visible : ViewStates.Gone;
            buttonSave.Enabled = !isLoading;
            buttonGeneratePassword.Enabled = !isLoading;
            editTextSiteName.Enabled = !isLoading;
            editTextUsername.Enabled = !isLoading;
            editTextPassword.Enabled = !isLoading;
            editTextConfirmPassword.Enabled = !isLoading;
            editTextNotes.Enabled = !isLoading;
        }
    }
}
