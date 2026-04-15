using Android.App;
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
        private TextView textViewUsernameError;
        private TextView textViewEmailError;
        private TextView textViewPasswordError;
        private TextView textViewPasswordStrength;
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
            editTextUsername = FindViewById<TextInputEditText>(Resource.Id.editTextUsername);
            editTextEmail = FindViewById<TextInputEditText>(Resource.Id.editTextEmail);
            editTextPassword = FindViewById<TextInputEditText>(Resource.Id.editTextPassword);
            editTextConfirmPassword = FindViewById<TextInputEditText>(Resource.Id.editTextConfirmPassword);
            buttonSignUp = FindViewById<MaterialButton>(Resource.Id.buttonSignUp);
            textViewUsernameError = FindViewById<TextView>(Resource.Id.textViewUsernameError);
            textViewEmailError = FindViewById<TextView>(Resource.Id.textViewEmailError);
            textViewPasswordError = FindViewById<TextView>(Resource.Id.textViewPasswordError);
            textViewPasswordStrength = FindViewById<TextView>(Resource.Id.textViewPasswordStrength);
            textViewConfirmPasswordError = FindViewById<TextView>(Resource.Id.textViewConfirmPasswordError);
            textViewGeneralError = FindViewById<TextView>(Resource.Id.textViewGeneralError);
            textViewLoginLink = FindViewById<TextView>(Resource.Id.textViewLoginLink);
            progressBarSignup = FindViewById<ProgressBar>(Resource.Id.progressBarSignup);
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

            // Real-time validation feedback as the user types
            editTextUsername.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidateUsername(editTextUsername.Text?.Trim());
                if (!result.IsValid)
                    ShowError(textViewUsernameError, result.ErrorMessage);
                else
                    HideError(textViewUsernameError);
            }));

            editTextEmail.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidateEmail(editTextEmail.Text?.Trim());
                if (!result.IsValid)
                    ShowError(textViewEmailError, result.ErrorMessage);
                else
                    HideError(textViewEmailError);
            }));

            editTextPassword.AddTextChangedListener(new SimpleTextWatcher(text =>
            {
                // Validate strength and show indicator
                UpdatePasswordStrengthIndicator(text);

                // Re-validate confirm password if it already has content
                if (!string.IsNullOrEmpty(editTextConfirmPassword.Text))
                {
                    var matchResult = ValidationHelper.ValidatePasswordsMatch(text, editTextConfirmPassword.Text);
                    if (!matchResult.IsValid)
                        ShowError(textViewConfirmPasswordError, matchResult.ErrorMessage);
                    else
                        HideError(textViewConfirmPasswordError);
                }
            }));

            editTextConfirmPassword.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidatePasswordsMatch(
                    editTextPassword.Text, editTextConfirmPassword.Text);
                if (!result.IsValid)
                    ShowError(textViewConfirmPasswordError, result.ErrorMessage);
                else
                    HideError(textViewConfirmPasswordError);
            }));
        }

        private void UpdatePasswordStrengthIndicator(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                textViewPasswordStrength.Visibility = ViewStates.Gone;
                HideError(textViewPasswordError);
                return;
            }

            // Show inline validation error if applicable
            var passResult = ValidationHelper.ValidatePassword(password);
            if (!passResult.IsValid)
                ShowError(textViewPasswordError, passResult.ErrorMessage);
            else
                HideError(textViewPasswordError);

            // Show strength indicator
            var strength = ValidationHelper.GetPasswordStrength(password);
            textViewPasswordStrength.Visibility = ViewStates.Visible;

            switch (strength)
            {
                case PasswordStrength.Weak:
                    textViewPasswordStrength.Text = GetString(Resource.String.signup_password_strength_weak);
                    textViewPasswordStrength.SetTextColor(new Android.Graphics.Color(
                        Resources.GetColor(Resource.Color.passwordStrengthWeak)));
                    break;
                case PasswordStrength.Fair:
                    textViewPasswordStrength.Text = GetString(Resource.String.signup_password_strength_fair);
                    textViewPasswordStrength.SetTextColor(new Android.Graphics.Color(
                        Resources.GetColor(Resource.Color.passwordStrengthFair)));
                    break;
                case PasswordStrength.Strong:
                    textViewPasswordStrength.Text = GetString(Resource.String.signup_password_strength_strong);
                    textViewPasswordStrength.SetTextColor(new Android.Graphics.Color(
                        Resources.GetColor(Resource.Color.passwordStrengthStrong)));
                    break;
                case PasswordStrength.VeryStrong:
                    textViewPasswordStrength.Text = GetString(Resource.String.signup_password_strength_very_strong);
                    textViewPasswordStrength.SetTextColor(new Android.Graphics.Color(
                        Resources.GetColor(Resource.Color.passwordStrengthVeryStrong)));
                    break;
            }
        }

        private async Task OnSignUpClicked()
        {
            ClearErrors();

            string username = editTextUsername.Text?.Trim();
            string email = editTextEmail.Text?.Trim();
            string password = editTextPassword.Text;
            string confirmPassword = editTextConfirmPassword.Text;

            if (!ValidateInputs(username, email, password, confirmPassword))
                return;

            SetLoadingState(true);

            try
            {
                string authSalt = EncryptionHelper.GenerateSalt();
                string encryptionSalt = EncryptionHelper.GenerateSalt();
                string masterPasswordKey = EncryptionHelper.DeriveKey(password, authSalt);

                var newUser = new User
                {
                    Username = username,
                    Email = email,
                    MasterPasswordKey = masterPasswordKey,
                    AuthSalt = authSalt,
                    EncryptionSalt = encryptionSalt
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
            if (!usernameResult.IsValid)
            {
                ShowError(textViewUsernameError, usernameResult.ErrorMessage);
                isValid = false;
            }

            var emailResult = ValidationHelper.ValidateEmail(email);
            if (!emailResult.IsValid)
            {
                ShowError(textViewEmailError, emailResult.ErrorMessage);
                isValid = false;
            }

            var passwordResult = ValidationHelper.ValidatePassword(password);
            if (!passwordResult.IsValid)
            {
                ShowError(textViewPasswordError, passwordResult.ErrorMessage);
                isValid = false;
            }

            var confirmResult = ValidationHelper.ValidatePasswordsMatch(password, confirmPassword);
            if (!confirmResult.IsValid)
            {
                ShowError(textViewConfirmPasswordError, confirmResult.ErrorMessage);
                isValid = false;
            }

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
            buttonSignUp.Enabled = !isLoading;
            editTextUsername.Enabled = !isLoading;
            editTextEmail.Enabled = !isLoading;
            editTextPassword.Enabled = !isLoading;
            editTextConfirmPassword.Enabled = !isLoading;
        }

    }
}
