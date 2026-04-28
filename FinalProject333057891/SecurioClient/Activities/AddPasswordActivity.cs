using Android.App;
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
    /// <summary>Activity for adding a new password entry to the vault using the shared activity_entry.xml layout.</summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AddPasswordActivity : AppCompatActivity
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

        // ── Views ──────────────────────────────────────────────
        private ImageView imageViewBack;
        private TextView textViewTitle;
        private TextView textViewSubtitle;

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

        // Holds existing entries for duplicate checking.
        private List<VaultItem> existingEntries = new List<VaultItem>();

        // ── Lifecycle ──────────────────────────────────────────

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

        // ── Setup helpers ──────────────────────────────────────

        /// <summary>Finds and assigns all view references from the layout.</summary>
        private void InitializeViews()
        {
            imageViewBack = FindViewById<ImageView>(Resource.Id.imageViewEntryBack);
            textViewTitle = FindViewById<TextView>(Resource.Id.textViewEntryTitle);
            textViewSubtitle = FindViewById<TextView>(Resource.Id.textViewEntrySubtitle);

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

        /// <summary>Sets the title, subtitle, and button label for add mode.</summary>
        private void ConfigureForAddMode()
        {
            textViewTitle.Text = GetString(Resource.String.entry_title_add);
            textViewSubtitle.Text = GetString(Resource.String.entry_subtitle_add);
            buttonSave.Text = GetString(Resource.String.entry_button_save);
        }

        /// <summary>Loads existing vault entries from the cache for duplicate checking.</summary>
        private void PopulateExistingEntries()
        {
            existingEntries = VaultEntryCache.Entries ?? new List<VaultItem>();
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

        // ── Save logic ─────────────────────────────────────────

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
                // Encrypt the password with AES-GCM using the session vault key.
                string vaultKey = SessionHelper.SessionVaultKey;
                var (iv, tag, cipherText) = EncryptionHelper.EncryptAesGcm(password, vaultKey);
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
            catch (Exception ex)
            {
                FormUiHelper.ShowError(textViewGeneralError, GetString(Resource.String.entry_error_save_failed));
                System.Diagnostics.Debug.WriteLine($"[ADD PASSWORD ERROR] {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        // ── Validation ─────────────────────────────────────────

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
            return existingEntries.Any(e =>
                string.Equals(e.AccountName, siteName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.AccountUsername, username, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Validates that the confirm password field matches the password field and shows or hides the error accordingly.</summary>
        private void ValidatePasswordsMatch()
        {
            string password = editTextPassword.Text;
            string confirm = editTextConfirmPassword.Text;
            if (string.IsNullOrEmpty(confirm))
            {
                FormUiHelper.HideError(textViewConfirmPasswordError);
                return;
            }
            if (password != confirm)
                FormUiHelper.ShowError(textViewConfirmPasswordError, GetString(Resource.String.entry_error_passwords_mismatch));
            else
                FormUiHelper.HideError(textViewConfirmPasswordError);
        }

        // ── Password strength indicator (informational) ────────

        /// <summary>Updates the password strength progress bar and hint text based on the given password.</summary>
        private void UpdatePasswordStrengthIndicator(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                progressBarStrength.Visibility = ViewStates.Gone;
                textViewStrengthHint.Visibility = ViewStates.Gone;
                FormUiHelper.HideError(textViewPasswordError);
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

        /// <summary>Maps a password score (1–5) to the corresponding color resource ID.</summary>
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

        /// <summary>Hides all field-level and general error messages.</summary>
        private void ClearErrors()
        {
            FormUiHelper.HideError(textViewSiteNameError);
            FormUiHelper.HideError(textViewUsernameError);
            FormUiHelper.HideError(textViewPasswordError);
            FormUiHelper.HideError(textViewConfirmPasswordError);
            FormUiHelper.HideError(textViewGeneralError);
        }

        /// <summary>Toggles the loading state, showing the progress bar and disabling form controls.</summary>
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

    /// <summary>Static cache used to share the current entry list between VaultActivity and entry activities for duplicate checking.</summary>
    public static class VaultEntryCache
    {
        public static List<VaultItem> Entries { get; set; } = new List<VaultItem>();
    }
}
