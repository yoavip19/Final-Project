using Android.App;
using Android.Content.Res;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using SecurioModels.DataTransferObjects;
using System;
using System.Threading.Tasks;

namespace SecurioClient
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class SignupActivity : AppCompatActivity
    {
        private TextInputEditText editTextUsername;
        private TextInputEditText editTextEmail;
        private TextInputEditText editTextPassword;
        private TextInputEditText editTextConfirmPassword;
        private MaterialButton buttonSignUp;
        private MaterialButton buttonGeneratePassword;
        private TextView textViewUsernameError;
        private TextView textViewEmailError;
        private TextView textViewPasswordError;
        private ProgressBar progressBarPasswordStrength;
        private TextView textViewPasswordHint;
        private TextView textViewConfirmPasswordError;
        private TextView textViewGeneralError;
        private TextView textViewLoginLink;
        private ProgressBar progressBarSignup;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_signup);

            InitializeViews();
            SetupEventHandlers();
        }

        private void InitializeViews()
        {
            editTextUsername         = FindViewById<TextInputEditText>(Resource.Id.editTextUsername);
            editTextEmail            = FindViewById<TextInputEditText>(Resource.Id.editTextEmail);
            editTextPassword         = FindViewById<TextInputEditText>(Resource.Id.editTextPassword);
            editTextConfirmPassword  = FindViewById<TextInputEditText>(Resource.Id.editTextConfirmPassword);
            buttonSignUp             = FindViewById<MaterialButton>(Resource.Id.buttonSignUp);
            buttonGeneratePassword   = FindViewById<MaterialButton>(Resource.Id.buttonGeneratePassword);
            textViewUsernameError    = FindViewById<TextView>(Resource.Id.textViewUsernameError);
            textViewEmailError       = FindViewById<TextView>(Resource.Id.textViewEmailError);
            textViewPasswordError    = FindViewById<TextView>(Resource.Id.textViewPasswordError);
            progressBarPasswordStrength = FindViewById<ProgressBar>(Resource.Id.progressBarPasswordStrength);
            textViewPasswordHint     = FindViewById<TextView>(Resource.Id.textViewPasswordHint);
            textViewConfirmPasswordError = FindViewById<TextView>(Resource.Id.textViewConfirmPasswordError);
            textViewGeneralError     = FindViewById<TextView>(Resource.Id.textViewGeneralError);
            textViewLoginLink        = FindViewById<TextView>(Resource.Id.textViewLoginLink);
            progressBarSignup        = FindViewById<ProgressBar>(Resource.Id.progressBarSignup);
        }

        private void SetupEventHandlers()
        {
            buttonSignUp.Click += async (sender, e) => await OnSignUpClicked();

            textViewLoginLink.Click += (sender, e) =>
            {
                var intent = new Android.Content.Intent(this, typeof(LoginActivity));
                StartActivity(intent);
                Finish();
            };

            // Generate a strong password and fill ONLY the password field.
            buttonGeneratePassword.Click += (sender, e) =>
            {
                string generated = ValidationHelper.GenerateStrongPassword();
                editTextPassword.Text = generated;
                // Confirm password intentionally left blank — user must type it themselves.
            };

            // Real-time validation feedback as the user types
            editTextUsername.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidateUsername(editTextUsername.Text?.Trim());
                if (!result.IsValid) ShowError(textViewUsernameError, result.ErrorMessage);
                else                 HideError(textViewUsernameError);
            }));

            editTextEmail.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidateEmail(editTextEmail.Text?.Trim());
                if (!result.IsValid) ShowError(textViewEmailError, result.ErrorMessage);
                else                 HideError(textViewEmailError);
            }));

            editTextPassword.AddTextChangedListener(new SimpleTextWatcher(text =>
            {
                UpdatePasswordStrengthIndicator(text);

                // Re-validate confirm password if the user has already typed in it.
                if (!string.IsNullOrEmpty(editTextConfirmPassword.Text))
                {
                    var matchResult = ValidationHelper.ValidatePasswordsMatch(text, editTextConfirmPassword.Text);
                    if (!matchResult.IsValid) ShowError(textViewConfirmPasswordError, matchResult.ErrorMessage);
                    else                      HideError(textViewConfirmPasswordError);
                }
            }));

            editTextConfirmPassword.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidatePasswordsMatch(
                    editTextPassword.Text, editTextConfirmPassword.Text);
                if (!result.IsValid) ShowError(textViewConfirmPasswordError, result.ErrorMessage);
                else                 HideError(textViewConfirmPasswordError);
            }));
        }

        // Updates the 5-segment strength ProgressBar and the constructive hint label
        // whenever the password field changes.
        private void UpdatePasswordStrengthIndicator(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                progressBarPasswordStrength.Visibility = ViewStates.Gone;
                textViewPasswordHint.Visibility = ViewStates.Gone;
                HideError(textViewPasswordError);
                return;
            }

            // Inline validation error (first unmet rule)
            var passResult = ValidationHelper.ValidatePassword(password);
            if (!passResult.IsValid) ShowError(textViewPasswordError, passResult.ErrorMessage);
            else                     HideError(textViewPasswordError);

            // Strength bar — progress = number of criteria met (0-5)
            int score = ValidationHelper.GetPasswordScore(password);
            progressBarPasswordStrength.Visibility = ViewStates.Visible;
            progressBarPasswordStrength.Progress = score;
            progressBarPasswordStrength.ProgressTintList =
                ColorStateList.ValueOf(new Android.Graphics.Color(Resources.GetColor(ScoreToColorRes(score))));

            // Constructive hint below the bar
            string hint = ValidationHelper.GetMissingCriteriaHint(password);
            textViewPasswordHint.Text = hint;
            textViewPasswordHint.Visibility = ViewStates.Visible;
            textViewPasswordHint.SetTextColor(new Android.Graphics.Color(
                Resources.GetColor(score == 5
                    ? Resource.Color.passwordStrengthVeryStrong
                    : Resource.Color.signupHintText)));
        }

        // Maps a score 1-5 to the matching color resource for the progress bar tint.
        private static int ScoreToColorRes(int score)
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

        private async Task OnSignUpClicked()
        {
            ClearErrors();

            string username        = editTextUsername.Text?.Trim();
            string email           = editTextEmail.Text?.Trim();
            string password        = editTextPassword.Text;
            string confirmPassword = editTextConfirmPassword.Text;

            if (!ValidateInputs(username, email, password, confirmPassword))
                return;

            SetLoadingState(true);

            try
            {
                string authSalt       = EncryptionHelper.GenerateSalt();
                string encryptionSalt = EncryptionHelper.GenerateSalt();
                string masterPasswordKey = EncryptionHelper.DeriveKey(password, authSalt);

                var newUser = new User
                {
                    Username         = username,
                    Email            = email,
                    MasterPasswordKey = masterPasswordKey,
                    AuthSalt         = authSalt,
                    EncryptionSalt   = encryptionSalt
                };

                var authService = new AuthService();
                var result = await authService.RegisterAsync(newUser);

                if (result.Success)
                {
                    var intent = new Android.Content.Intent(this, typeof(MainActivity));
                    intent.SetFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.ClearTask);
                    StartActivity(intent);
                    Finish();
                }
                else
                {
                    ShowGeneralError(result.Message);
                }
            }
            catch (Exception ex)
            {
                ShowGeneralError("Unable to create account. Please check your connection and try again.");
                System.Diagnostics.Debug.WriteLine($"[SIGNUP ERROR] {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private bool ValidateInputs(string username, string email, string password, string confirmPassword)
        {
            bool isValid = true;

            var usernameResult = ValidationHelper.ValidateUsername(username);
            if (!usernameResult.IsValid) { ShowError(textViewUsernameError, usernameResult.ErrorMessage); isValid = false; }

            var emailResult = ValidationHelper.ValidateEmail(email);
            if (!emailResult.IsValid) { ShowError(textViewEmailError, emailResult.ErrorMessage); isValid = false; }

            var passwordResult = ValidationHelper.ValidatePassword(password);
            if (!passwordResult.IsValid) { ShowError(textViewPasswordError, passwordResult.ErrorMessage); isValid = false; }

            var confirmResult = ValidationHelper.ValidatePasswordsMatch(password, confirmPassword);
            if (!confirmResult.IsValid) { ShowError(textViewConfirmPasswordError, confirmResult.ErrorMessage); isValid = false; }

            return isValid;
        }

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

        private void ShowGeneralError(string message)
        {
            textViewGeneralError.Text = message;
            textViewGeneralError.Visibility = ViewStates.Visible;
        }

        private void ClearErrors()
        {
            HideError(textViewUsernameError);
            HideError(textViewEmailError);
            HideError(textViewPasswordError);
            HideError(textViewConfirmPasswordError);
            HideError(textViewGeneralError);
        }

        private void SetLoadingState(bool isLoading)
        {
            progressBarSignup.Visibility = isLoading ? ViewStates.Visible : ViewStates.Gone;
            buttonSignUp.Enabled             = !isLoading;
            buttonGeneratePassword.Enabled   = !isLoading;
            editTextUsername.Enabled         = !isLoading;
            editTextEmail.Enabled            = !isLoading;
            editTextPassword.Enabled         = !isLoading;
            editTextConfirmPassword.Enabled  = !isLoading;
        }
    }
}
