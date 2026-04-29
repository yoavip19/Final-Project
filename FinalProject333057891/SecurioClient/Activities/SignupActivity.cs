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

namespace SecurioClient.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    /// <summary>Activity that provides the registration form and creates a new user account.</summary>
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

        /// <summary>Initializes the activity, inflates the layout, and wires up views and handlers.</summary>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_signup);

            InitializeViews();
            SetupEventHandlers();
        }

        /// <summary>Finds and assigns all view references from the layout.</summary>
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

        /// <summary>Wires up click and text-change event handlers for signup form controls.</summary>
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
                if (!result.IsValid) FormUiHelper.ShowError(textViewUsernameError, result.ErrorMessage);
                else                 FormUiHelper.HideError(textViewUsernameError);
            }));

            editTextEmail.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidateEmail(editTextEmail.Text?.Trim());
                if (!result.IsValid) FormUiHelper.ShowError(textViewEmailError, result.ErrorMessage);
                else                 FormUiHelper.HideError(textViewEmailError);
            }));

            editTextPassword.AddTextChangedListener(new SimpleTextWatcher(text =>
            {
                FormUiHelper.UpdatePasswordStrengthIndicator(
                    text, progressBarPasswordStrength, textViewPasswordHint, textViewPasswordError, Resources);

                // Re-validate confirm password if the user has already typed in it.
                if (!string.IsNullOrEmpty(editTextConfirmPassword.Text))
                {
                    var matchResult = ValidationHelper.ValidatePasswordsMatch(text, editTextConfirmPassword.Text);
                    if (!matchResult.IsValid) FormUiHelper.ShowError(textViewConfirmPasswordError, matchResult.ErrorMessage);
                    else                      FormUiHelper.HideError(textViewConfirmPasswordError);
                }
            }));

            editTextConfirmPassword.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidatePasswordsMatch(
                    editTextPassword.Text, editTextConfirmPassword.Text);
                if (!result.IsValid) FormUiHelper.ShowError(textViewConfirmPasswordError, result.ErrorMessage);
                else                 FormUiHelper.HideError(textViewConfirmPasswordError);
            }));
        }

        /// <summary>Validates inputs and registers a new user via AuthService.</summary>
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
                // Compute SHA-1 of the plaintext password for the client-side HIBP breach check.
                string passwordSha1Hash = EncryptionHelper.ComputeSha1Hash(password);

                // Check HIBP before sending anything to the server — fail fast if the password is known to be breached.
                bool isPwned = await HibpClientService.IsPasswordPwnedAsync(passwordSha1Hash);
                if (isPwned)
                {
                    FormUiHelper.ShowError(textViewPasswordError,
                        "This password has appeared in a known data breach. Please choose a different password.");
                    return;
                }

                string authSalt       = EncryptionHelper.GenerateSalt();
                string encryptionSalt = EncryptionHelper.GenerateSalt();
                string masterPasswordKey = EncryptionHelper.DeriveKey(password, authSalt);

                var newUser = new User
                {
                    Username          = username,
                    Email             = email,
                    MasterPasswordKey = masterPasswordKey,
                    AuthSalt          = authSalt,
                    EncryptionSalt    = encryptionSalt
                };

                var authService = new AuthService();
                var result = await authService.RegisterAsync(newUser, password);

                if (result.Success)
                {
                    var intent = new Android.Content.Intent(this, typeof(VaultActivity));
                    intent.SetFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.ClearTask);
                    StartActivity(intent);
                    Finish();
                }
                else
                {
                    FormUiHelper.ShowError(textViewGeneralError, result.Message);
                }
            }
            catch (Exception)
            {
                FormUiHelper.ShowError(textViewGeneralError, GetString(Resource.String.signup_error_create_failed));
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>Validates all signup form fields and shows errors for any invalid values.</summary>
        private bool ValidateInputs(string username, string email, string password, string confirmPassword)
        {
            bool isValid = true;

            var usernameResult = ValidationHelper.ValidateUsername(username);
            if (!usernameResult.IsValid) { FormUiHelper.ShowError(textViewUsernameError, usernameResult.ErrorMessage); isValid = false; }

            var emailResult = ValidationHelper.ValidateEmail(email);
            if (!emailResult.IsValid) { FormUiHelper.ShowError(textViewEmailError, emailResult.ErrorMessage); isValid = false; }

            var passwordResult = ValidationHelper.ValidatePassword(password);
            if (!passwordResult.IsValid) { FormUiHelper.ShowError(textViewPasswordError, passwordResult.ErrorMessage); isValid = false; }

            var confirmResult = ValidationHelper.ValidatePasswordsMatch(password, confirmPassword);
            if (!confirmResult.IsValid) { FormUiHelper.ShowError(textViewConfirmPasswordError, confirmResult.ErrorMessage); isValid = false; }

            return isValid;
        }

        /// <summary>Hides all field-level and general error messages.</summary>
        private void ClearErrors()
        {
            FormUiHelper.HideError(textViewUsernameError);
            FormUiHelper.HideError(textViewEmailError);
            FormUiHelper.HideError(textViewPasswordError);
            FormUiHelper.HideError(textViewConfirmPasswordError);
            FormUiHelper.HideError(textViewGeneralError);
        }

        /// <summary>Toggles the loading state, showing the progress bar and disabling form controls.</summary>
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
