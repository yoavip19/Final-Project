using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using SecurioClient.Helpers;
using System;

namespace SecurioClient.Activities
{
    /// <summary>
    /// Shared base class for <see cref="AddPasswordActivity"/> and <see cref="EditPasswordActivity"/>.
    /// Provides common view references, UI helpers, and form-validation logic so each
    /// subclass only needs to implement mode-specific configuration and save/update behaviour.
    /// </summary>
    public abstract class BasePasswordEntryActivity : SecuredAppCompatActivity
    {
        // ── Shared view fields ─────────────────────────────────
        protected ImageView imageViewBack;
        protected TextView textViewTitle;
        protected TextView textViewSubtitle;

        protected TextInputEditText editTextSiteName;
        protected TextInputEditText editTextUsername;
        protected TextInputEditText editTextPassword;
        protected TextInputEditText editTextConfirmPassword;
        protected TextInputEditText editTextNotes;

        protected TextView textViewSiteNameError;
        protected TextView textViewUsernameError;
        protected TextView textViewPasswordError;
        protected TextView textViewConfirmPasswordError;
        protected TextView textViewGeneralError;

        protected ProgressBar progressBarStrength;
        protected TextView textViewStrengthHint;

        protected MaterialButton buttonGeneratePassword;
        protected MaterialButton buttonSave;
        protected ProgressBar progressBar;

        // ── View initialisation ────────────────────────────────

        /// <summary>Finds and assigns all view references that are shared between Add and Edit modes.</summary>
        protected virtual void InitializeViews()
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

        // ── Password strength indicator ────────────────────────

        /// <summary>Updates the password strength progress bar and hint text based on the given password.</summary>
        protected void UpdatePasswordStrengthIndicator(string password)
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
        protected static int ScoreToColorRes(int score)
        {
            switch (score)
            {
                case 1: return Resource.Color.passwordStrengthWeak;
                case 2: return Resource.Color.passwordStrengthPoor;
                case 3: return Resource.Color.passwordStrengthFair;
                case 4: return Resource.Color.passwordStrengthStrong;
                case 5: return Resource.Color.passwordStrengthVeryStrong;
                default: throw new ArgumentOutOfRangeException(nameof(score), score, "Password score must be between 1 and 5.");
            }
        }

        // ── Validation helpers ─────────────────────────────────

        /// <summary>Validates that the confirm password field matches the password field and shows or hides the error accordingly.</summary>
        protected void ValidatePasswordsMatch()
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

        // ── UI helpers ─────────────────────────────────────────

        /// <summary>Hides all field-level and general error messages.</summary>
        protected void ClearErrors()
        {
            FormUiHelper.HideError(textViewSiteNameError);
            FormUiHelper.HideError(textViewUsernameError);
            FormUiHelper.HideError(textViewPasswordError);
            FormUiHelper.HideError(textViewConfirmPasswordError);
            FormUiHelper.HideError(textViewGeneralError);
        }

        /// <summary>Toggles the loading state, showing the progress bar and disabling form controls.</summary>
        protected void SetLoadingState(bool isLoading)
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
