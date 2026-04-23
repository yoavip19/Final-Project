using Android.Content.Res;
using Android.Views;
using Android.Widget;

namespace SecurioClient.Helpers
{
    /// <summary>Shared UI helpers for form activities that display inline field errors and password-strength feedback.</summary>
    public static class FormUiHelper
    {
        /// <summary>Shows an inline error message below a form field.</summary>
        public static void ShowError(TextView errorView, string message)
        {
            errorView.Text = message;
            errorView.Visibility = ViewStates.Visible;
        }

        /// <summary>Hides an inline error message.</summary>
        public static void HideError(TextView errorView)
        {
            errorView.Text = null;
            errorView.Visibility = ViewStates.Gone;
        }

        /// <summary>Updates a 5-segment password-strength ProgressBar and constructive hint label.</summary>
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

        /// <summary>Maps a cumulative score (1-5) to the matching color resource for the progress bar tint.</summary>
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
