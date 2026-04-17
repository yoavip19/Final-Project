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

namespace SecurioClient
{
    /// <summary>
    /// Activity for adding a new password entry to the vault.
    /// Uses the shared <c>activity_entry.xml</c> layout.
    /// A separate Edit activity will reuse the same XML in the future.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AddPasswordActivity : AppCompatActivity
    {
        // Result extras returned to the caller.
        public const string ResultEntryId = "RESULT_ENTRY_ID";
        public const string ResultSiteName = "RESULT_SITE_NAME";
        public const string ResultUsername = "RESULT_USERNAME";
        public const string ResultNotes = "RESULT_NOTES";

        public const int RequestCodeAdd = 1001;

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

        // Holds existing entries for duplicate checking.
        private List<PasswordEntry> existingEntries = new List<PasswordEntry>();

        // ── Lifecycle ──────────────────────────────────────────

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

        private void ConfigureForAddMode()
        {
            textViewTitle.Text = GetString(Resource.String.entry_title_add);
            textViewSubtitle.Text = GetString(Resource.String.entry_subtitle_add);
            buttonSave.Text = GetString(Resource.String.entry_button_save);
        }

        /// <summary>
        /// Loads the existing vault entries so that duplicate-check works.
        /// </summary>
        private void PopulateExistingEntries()
        {
            existingEntries = VaultEntryCache.Entries ?? new List<PasswordEntry>();
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

            buttonSave.Click += async (s, e) => await OnSaveClicked();

            // Real-time validation: site name
            editTextSiteName.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                string site = editTextSiteName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(site))
                    ShowError(textViewSiteNameError, GetString(Resource.String.entry_error_site_required));
                else
                    HideError(textViewSiteNameError);

                // Clear any previous duplicate error when the user changes the field.
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

            // Real-time validation: password (strength bar — informational only, never blocks)
            editTextPassword.AddTextChangedListener(new SimpleTextWatcher(text =>
            {
                UpdatePasswordStrengthIndicator(text);
            }));
        }

        // ── Save logic ─────────────────────────────────────────

        private async Task OnSaveClicked()
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
                    AccountName = siteName,
                    AccountUsername = username,
                    IV = iv,
                    Tag = tag,
                    CipherText = cipherText,
                    Notes = notes ?? string.Empty,
                    Sha1Hash = sha1Hash,
                    LastUpdate = DateTime.UtcNow
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

                    SetResult(Result.Ok, resultIntent);

                    Toast.MakeText(this,
                        GetString(Resource.String.entry_saved_success),
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
                ShowError(textViewGeneralError, "Unable to save password. Please check your connection and try again.");
                System.Diagnostics.Debug.WriteLine($"[ADD PASSWORD ERROR] {ex.Message}");
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

            // NOTE: Weak passwords are intentionally allowed because the user cannot
            // control what password policy external sites enforce (e.g. a 4-digit PIN).

            return valid;
        }

        /// <summary>
        /// Returns <c>true</c> when an entry with the same site name AND username
        /// already exists in the vault.
        /// </summary>
        private bool IsDuplicate(string siteName, string username)
        {
            return existingEntries.Any(e =>
                string.Equals(e.SiteName, siteName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));
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

    /// <summary>
    /// Static cache used to share the current entry list between VaultActivity and
    /// entry activities (Add / Edit) so duplicate checking works without a database round-trip.
    /// </summary>
    public static class VaultEntryCache
    {
        public static List<PasswordEntry> Entries { get; set; } = new List<PasswordEntry>();
    }
}
