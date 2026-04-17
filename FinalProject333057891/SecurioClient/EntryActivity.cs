using Android.App;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using SecurioClient.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SecurioClient
{
    /// <summary>
    /// A single activity that handles both adding a new vault entry and editing an
    /// existing one.  The mode is determined by the presence of the <c>EXTRA_ENTRY_ID</c>
    /// intent extra:  absent → Add mode, present → Edit mode.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class EntryActivity : AppCompatActivity
    {
        // Intent extra keys used to pass data between activities.
        public const string ExtraEntryId = "EXTRA_ENTRY_ID";
        public const string ExtraSiteName = "EXTRA_SITE_NAME";
        public const string ExtraUsername = "EXTRA_USERNAME";
        public const string ExtraPassword = "EXTRA_PASSWORD";
        public const string ExtraUrl = "EXTRA_URL";
        public const string ExtraNotes = "EXTRA_NOTES";

        // Result extras returned to the caller.
        public const string ResultSiteName = "RESULT_SITE_NAME";
        public const string ResultUsername = "RESULT_USERNAME";
        public const string ResultPassword = "RESULT_PASSWORD";
        public const string ResultUrl = "RESULT_URL";
        public const string ResultNotes = "RESULT_NOTES";
        public const string ResultEntryId = "RESULT_ENTRY_ID";

        public const int RequestCodeAdd = 1001;
        public const int RequestCodeEdit = 1002;

        // ── Views ──────────────────────────────────────────────
        private ImageView imageViewBack;
        private TextView textViewTitle;
        private TextView textViewSubtitle;

        private TextInputEditText editTextSiteName;
        private TextInputEditText editTextUsername;
        private TextInputEditText editTextPassword;
        private TextInputEditText editTextUrl;
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

        // ── State ──────────────────────────────────────────────
        private bool isEditMode;
        private int editEntryId = -1;

        // Holds existing entries for duplicate checking.
        // In a real app this would come from a repository/service.
        private List<PasswordEntry> existingEntries = new List<PasswordEntry>();

        // ── Lifecycle ──────────────────────────────────────────

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_entry);

            DetermineMode();
            InitializeViews();
            ConfigureForMode();
            PopulateExistingEntries();
            SetupEventHandlers();
        }

        // ── Setup helpers ──────────────────────────────────────

        private void DetermineMode()
        {
            isEditMode = Intent.HasExtra(ExtraEntryId);
            if (isEditMode)
                editEntryId = Intent.GetIntExtra(ExtraEntryId, -1);
        }

        private void InitializeViews()
        {
            imageViewBack = FindViewById<ImageView>(Resource.Id.imageViewEntryBack);
            textViewTitle = FindViewById<TextView>(Resource.Id.textViewEntryTitle);
            textViewSubtitle = FindViewById<TextView>(Resource.Id.textViewEntrySubtitle);

            editTextSiteName = FindViewById<TextInputEditText>(Resource.Id.editTextEntrySiteName);
            editTextUsername = FindViewById<TextInputEditText>(Resource.Id.editTextEntryUsername);
            editTextPassword = FindViewById<TextInputEditText>(Resource.Id.editTextEntryPassword);
            editTextUrl = FindViewById<TextInputEditText>(Resource.Id.editTextEntryUrl);
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

        private void ConfigureForMode()
        {
            if (isEditMode)
            {
                textViewTitle.Text = GetString(Resource.String.entry_title_edit);
                textViewSubtitle.Text = GetString(Resource.String.entry_subtitle_edit);
                buttonSave.Text = GetString(Resource.String.entry_button_update);

                // Pre-fill the fields with the existing entry's data.
                editTextSiteName.Text = Intent.GetStringExtra(ExtraSiteName) ?? string.Empty;
                editTextUsername.Text = Intent.GetStringExtra(ExtraUsername) ?? string.Empty;
                editTextPassword.Text = Intent.GetStringExtra(ExtraPassword) ?? string.Empty;
                editTextUrl.Text = Intent.GetStringExtra(ExtraUrl) ?? string.Empty;
                editTextNotes.Text = Intent.GetStringExtra(ExtraNotes) ?? string.Empty;
            }
        }

        /// <summary>
        /// Loads the existing vault entries so that duplicate-check works.
        /// In a real implementation this would come from a local cache or server.
        /// For now, VaultActivity passes its entry list via a static helper.
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

            buttonSave.Click += (s, e) => OnSaveClicked();

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

        private void OnSaveClicked()
        {
            ClearErrors();

            string siteName = editTextSiteName.Text?.Trim();
            string username = editTextUsername.Text?.Trim();
            string password = editTextPassword.Text;
            string url = editTextUrl.Text?.Trim();
            string notes = editTextNotes.Text?.Trim();

            if (!ValidateInputs(siteName, username, password))
                return;

            if (IsDuplicate(siteName, username))
            {
                ShowError(textViewGeneralError, GetString(Resource.String.entry_error_duplicate));
                return;
            }

            // Return the data to VaultActivity so it can update its list.
            var resultIntent = new Android.Content.Intent();
            resultIntent.PutExtra(ResultSiteName, siteName);
            resultIntent.PutExtra(ResultUsername, username);
            resultIntent.PutExtra(ResultPassword, password);
            resultIntent.PutExtra(ResultUrl, url ?? string.Empty);
            resultIntent.PutExtra(ResultNotes, notes ?? string.Empty);

            if (isEditMode)
                resultIntent.PutExtra(ResultEntryId, editEntryId);

            SetResult(Result.Ok, resultIntent);

            Toast.MakeText(this,
                GetString(isEditMode ? Resource.String.entry_updated_success : Resource.String.entry_saved_success),
                ToastLength.Short).Show();

            Finish();
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
        /// already exists in the vault, excluding the entry currently being edited.
        /// </summary>
        private bool IsDuplicate(string siteName, string username)
        {
            return existingEntries.Any(e =>
                e.Id != editEntryId
                && string.Equals(e.SiteName, siteName, StringComparison.OrdinalIgnoreCase)
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
    }

    /// <summary>
    /// Static cache used to share the current entry list between VaultActivity and
    /// EntryActivity so duplicate checking works without a database round-trip.
    /// </summary>
    public static class VaultEntryCache
    {
        public static List<PasswordEntry> Entries { get; set; } = new List<PasswordEntry>();
    }
}
