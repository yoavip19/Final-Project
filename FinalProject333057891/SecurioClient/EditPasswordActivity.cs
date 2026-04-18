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

namespace SecurioClient
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

        // Result extras returned to the caller.
        public const string ResultEntryId = "RESULT_ENTRY_ID";
        public const string ResultSiteName = "RESULT_SITE_NAME";
        public const string ResultUsername = "RESULT_USERNAME";
        public const string ResultNotes = "RESULT_NOTES";

        public const int RequestCodeEdit = 1002;

        // ── Views ──────────────────────────────────────────────
        private ImageView imageViewBack;
        private TextView textViewTitle;
        private TextView textViewSubtitle;

        private TextInputEditText editTextSiteName;
        private TextInputEditText editTextUsername;
        private TextInputEditText editTextPassword;
        private TextInputEditText editTextNotes;

        private TextView textViewSiteNameError;
        private TextView textViewUsernameError;
        private TextView textViewPasswordError;
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
        }

        // ── Setup helpers ──────────────────────────────────────

        private void InitializeViews()
        {
            imageViewBack = FindViewById<ImageView>(Resource.Id.imageViewEntryBack);
            textViewTitle = FindViewById<TextView>(Resource.Id.textViewEntryTitle);
            textViewSubtitle = FindViewById<TextView>(Resource.Id.textViewEntrySubtitle);

            editTextSiteName = FindViewById<TextInputEditText>(Resource.Id.editTextEntrySiteName);
            editTextUsername = FindViewById<TextInputEditText>(Resource.Id.editTextEntryUsername);
            editTextPassword = FindViewById<TextInputEditText>(Resource.Id.editTextEntryPassword);
            editTextNotes = FindViewById<TextInputEditText>(Resource.Id.editTextEntryNotes);

            textViewSiteNameError = FindViewById<TextView>(Resource.Id.textViewEntrySiteNameError);
            textViewUsernameError = FindViewById<TextView>(Resource.Id.textViewEntryUsernameError);
            textViewPasswordError = FindViewById<TextView>(Resource.Id.textViewEntryPasswordError);
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
        /// The password field is left blank because the vault stores only the encrypted
        /// form — the user must re-enter or generate a new password.
        /// </summary>
        private void PopulateFieldsFromIntent()
        {
            editTextSiteName.Text = Intent.GetStringExtra(ExtraSiteName) ?? string.Empty;
            editTextUsername.Text = Intent.GetStringExtra(ExtraUsername) ?? string.Empty;
            editTextNotes.Text = Intent.GetStringExtra(ExtraNotes) ?? string.Empty;
            // Password is intentionally left empty — the vault only stores encrypted ciphertext.
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
                editTextPassword.Text = ValidationHelper.GenerateStrongPassword();
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
                UpdatePasswordStrengthIndicator(text);
            }));
        }

        // ── Update logic ───────────────────────────────────────

        private async Task OnUpdateClicked()
        {
            ClearErrors();

            string siteName = editTextSiteName.Text?.Trim();
            string username = editTextUsername.Text?.Trim();
            string password = editTextPassword.Text;
            string notes = editTextNotes.Text?.Trim();

            if (!ValidateInputs(siteName, username, password))
                return;

            if (IsDuplicate(siteName, username))
            {
                ShowError(textViewGeneralError, GetString(Resource.String.entry_error_duplicate));
                return;
            }

            SetLoadingState(true);

            try
            {
                // Encrypt the password with AES-GCM using the session vault key.
                string vaultKey = SessionHelper.SessionVaultKey;
                var (iv, tag, cipherText) = EncryptionHelper.EncryptAesGcm(password, vaultKey);
                string sha1Hash = EncryptionHelper.ComputeSha1Hash(password);

                var vaultItem = new VaultItem
                {
                    Id = entryId,
                    AccountName = siteName,
                    AccountUsername = username,
                    IV = iv,
                    Tag = tag,
                    CipherText = cipherText,
                    Notes = notes ?? string.Empty,
                    Sha1Hash = sha1Hash
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
                ShowError(textViewGeneralError, "Unable to update password. Please check your connection and try again.");
                System.Diagnostics.Debug.WriteLine($"[EDIT PASSWORD ERROR] {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        // ── Validation ─────────────────────────────────────────

        private bool ValidateInputs(string siteName, string username, string password)
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

            if (string.IsNullOrEmpty(password))
            {
                ShowError(textViewPasswordError, GetString(Resource.String.entry_error_password_required));
                valid = false;
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
            editTextNotes.Enabled = !isLoading;
        }
    }
}
