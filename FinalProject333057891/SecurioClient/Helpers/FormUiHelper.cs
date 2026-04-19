using Android.Content.Res;
using Android.Views;
using Android.Widget;

namespace SecurioClient.Helpers
{
    // Shared UI helpers for form activities that display inline field errors and
    // password-strength feedback.  Used by SignupActivity and EditAccountActivity
    // to follow DRY — all validation display logic lives here.
    public static class FormUiHelper
    {
        // Shows an inline error message below a form field.
        public static void ShowError(TextView errorView, string message)
        {
            errorView.Text = message;
            errorView.Visibility = ViewStates.Visible;
        }

        // Hides an inline error message.
        public static void HideError(TextView errorView)
        {
            errorView.Text = null;
            errorView.Visibility = ViewStates.Gone;
        }

        // Updates a 5-segment password-strength ProgressBar and constructive hint label.
        // Performs inline validation via ValidationHelper.
        public static void UpdatePasswordStrengthIndicator(
            string password,
            ProgressBar strengthBar,
            TextView hintLabel,
            TextView errorLabel,
            Android.Content.Res.Resources resources)
        {
            if (string.IsNullOrEmpty(password))
            {
                strengthBar.Visibility = ViewStates.Gone;
                hintLabel.Visibility   = ViewStates.Gone;
                HideError(errorLabel);
                return;
            }

            // Inline validation error (first unmet rule)
            var passResult = ValidationHelper.ValidatePassword(password);
            if (!passResult.IsValid) ShowError(errorLabel, passResult.ErrorMessage);
            else                     HideError(errorLabel);

            // Strength bar — progress = number of criteria met (0-5)
            int score = ValidationHelper.GetPasswordScore(password);
            strengthBar.Visibility = ViewStates.Visible;
            strengthBar.Progress   = score;
            strengthBar.ProgressTintList =
                ColorStateList.ValueOf(new Android.Graphics.Color(resources.GetColor(ScoreToColorRes(score))));

            // Constructive hint below the bar
            string hint = ValidationHelper.GetMissingCriteriaHint(password);
            hintLabel.Text       = hint;
            hintLabel.Visibility = ViewStates.Visible;
            hintLabel.SetTextColor(new Android.Graphics.Color(
                resources.GetColor(score == 5
                    ? Resource.Color.passwordStrengthVeryStrong
                    : Resource.Color.signupHintText)));
        }

        // Maps a cumulative score (1-5) to the matching color resource for the progress bar tint.
        public static int ScoreToColorRes(int score)
        {
            switch (score)
            {
                case 1:  return Resource.Color.passwordStrengthWeak;
                case 2:  return Resource.Color.passwordStrengthPoor;
                case 3:  return Resource.Color.passwordStrengthFair;
                case 4:  return Resource.Color.passwordStrengthStrong;
                default: return Resource.Color.passwordStrengthVeryStrong;
            }
        }
    }
}
