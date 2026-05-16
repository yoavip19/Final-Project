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
    /// <summary>Shared base class for AddPasswordActivity and EditPasswordActivity that provides common view references, UI helpers, and form-validation logic.</summary>
    public abstract class BasePasswordEntryActivity : SecuredAppCompatActivity
    {
        // -- Shared view properties (subclass-readable, base-class-assigned) --
        protected ImageView ImageViewBack { get; private set; }
        protected TextView TextViewTitle { get; private set; }
        protected TextView TextViewSubtitle { get; private set; }

        /// <summary>Input field for the account/site name.</summary>
        protected TextInputEditText EditTextSiteName { get; private set; }
        /// <summary>Input field for the account username.</summary>
        protected TextInputEditText EditTextUsername { get; private set; }
        /// <summary>Input field for the password.</summary>
        protected TextInputEditText EditTextPassword { get; private set; }
        /// <summary>Input field for confirming the password.</summary>
        protected TextInputEditText EditTextConfirmPassword { get; private set; }
        /// <summary>Input field for optional notes.</summary>
        protected TextInputEditText EditTextNotes { get; private set; }

        /// <summary>Error label for the site name field.</summary>
        protected TextView TextViewSiteNameError { get; private set; }
        /// <summary>Error label for the username field.</summary>
        protected TextView TextViewUsernameError { get; private set; }
        /// <summary>Error label for the password field.</summary>
        protected TextView TextViewPasswordError { get; private set; }
        /// <summary>Error label for the confirm password field.</summary>
        protected TextView TextViewConfirmPasswordError { get; private set; }
        /// <summary>General error label shown below the form.</summary>
        protected TextView TextViewGeneralError { get; private set; }

        /// <summary>Button that auto-generates a strong password.</summary>
        protected MaterialButton ButtonGeneratePassword { get; private set; }
        protected MaterialButton ButtonSave { get; private set; }

        // -- Base-class-internal view properties ----------------
        private ProgressBar ProgressBarStrength { get; set; }
        private TextView TextViewStrengthHint { get; set; }
        private ProgressBar ProgressBarEntry { get; set; }

        // -- View initialisation --------------------------------

        /// <summary>Finds and assigns all view references that are shared between Add and Edit modes.</summary>
        protected virtual void InitializeViews()
        {
            ImageViewBack = FindViewById<ImageView>(Resource.Id.imageViewEntryBack);
            TextViewTitle = FindViewById<TextView>(Resource.Id.textViewEntryTitle);
            TextViewSubtitle = FindViewById<TextView>(Resource.Id.textViewEntrySubtitle);

            EditTextSiteName = FindViewById<TextInputEditText>(Resource.Id.editTextEntrySiteName);
            EditTextUsername = FindViewById<TextInputEditText>(Resource.Id.editTextEntryUsername);
            EditTextPassword = FindViewById<TextInputEditText>(Resource.Id.editTextEntryPassword);
            EditTextConfirmPassword = FindViewById<TextInputEditText>(Resource.Id.editTextEntryConfirmPassword);
            EditTextNotes = FindViewById<TextInputEditText>(Resource.Id.editTextEntryNotes);

            TextViewSiteNameError = FindViewById<TextView>(Resource.Id.textViewEntrySiteNameError);
            TextViewUsernameError = FindViewById<TextView>(Resource.Id.textViewEntryUsernameError);
            TextViewPasswordError = FindViewById<TextView>(Resource.Id.textViewEntryPasswordError);
            TextViewConfirmPasswordError = FindViewById<TextView>(Resource.Id.textViewEntryConfirmPasswordError);
            TextViewGeneralError = FindViewById<TextView>(Resource.Id.textViewEntryGeneralError);

            ProgressBarStrength = FindViewById<ProgressBar>(Resource.Id.progressBarEntryStrength);
            TextViewStrengthHint = FindViewById<TextView>(Resource.Id.textViewEntryStrengthHint);

            ButtonGeneratePassword = FindViewById<MaterialButton>(Resource.Id.buttonEntryGeneratePassword);
            ButtonSave = FindViewById<MaterialButton>(Resource.Id.buttonEntrySave);
            ProgressBarEntry = FindViewById<ProgressBar>(Resource.Id.progressBarEntry);
        }

        // -- Password strength indicator ------------------------

        /// <summary>Updates the password strength progress bar and hint text based on the given password.</summary>
        protected void UpdatePasswordStrengthIndicator(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                ProgressBarStrength.Visibility = ViewStates.Gone;
                TextViewStrengthHint.Visibility = ViewStates.Gone;
                FormUiHelper.HideError(TextViewPasswordError);
                return;
            }

            int score = ValidationHelper.GetPasswordScore(password);
            ProgressBarStrength.Visibility = ViewStates.Visible;
            ProgressBarStrength.Progress = score;
            ProgressBarStrength.ProgressTintList =
                ColorStateList.ValueOf(new Android.Graphics.Color(Resources.GetColor(ScoreToColorRes(score))));

            string hint = ValidationHelper.GetMissingCriteriaHint(password);
            TextViewStrengthHint.Text = hint;
            TextViewStrengthHint.Visibility = ViewStates.Visible;
            TextViewStrengthHint.SetTextColor(new Android.Graphics.Color(
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

        // -- Validation helpers ---------------------------------

        /// <summary>Validates that the confirm password field matches the password field and shows or hides the error accordingly.</summary>
        protected void ValidatePasswordsMatch()
        {
            string password = EditTextPassword.Text;
            string confirm = EditTextConfirmPassword.Text;
            if (string.IsNullOrEmpty(confirm))
            {
                FormUiHelper.HideError(TextViewConfirmPasswordError);
                return;
            }
            if (password != confirm)
                FormUiHelper.ShowError(TextViewConfirmPasswordError, GetString(Resource.String.entry_error_passwords_mismatch));
            else
                FormUiHelper.HideError(TextViewConfirmPasswordError);
        }

        // -- UI helpers -----------------------------------------

        /// <summary>Hides all field-level and general error messages.</summary>
        protected void ClearErrors()
        {
            FormUiHelper.HideError(TextViewSiteNameError);
            FormUiHelper.HideError(TextViewUsernameError);
            FormUiHelper.HideError(TextViewPasswordError);
            FormUiHelper.HideError(TextViewConfirmPasswordError);
            FormUiHelper.HideError(TextViewGeneralError);
        }

        /// <summary>Toggles the loading state, showing the progress bar and disabling form controls.</summary>
        protected void SetLoadingState(bool isLoading)
        {
            ProgressBarEntry.Visibility = isLoading ? ViewStates.Visible : ViewStates.Gone;
            ButtonSave.Enabled = !isLoading;
            ButtonGeneratePassword.Enabled = !isLoading;
            EditTextSiteName.Enabled = !isLoading;
            EditTextUsername.Enabled = !isLoading;
            EditTextPassword.Enabled = !isLoading;
            EditTextConfirmPassword.Enabled = !isLoading;
            EditTextNotes.Enabled = !isLoading;
        }
    }
}
