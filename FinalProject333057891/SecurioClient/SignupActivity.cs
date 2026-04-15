using Android.App;
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
using System.Text.RegularExpressions;
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
                // Navigate back to login (finish this activity)
                Finish();
            };
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
                // Generate cryptographic salts for this user
                string authSalt = EncryptionHelper.GenerateSalt();
                string encryptionSalt = EncryptionHelper.GenerateSalt();

                // Derive the master password key using the auth salt
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
                    // Registration successful – navigate to the main activity
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
                ShowGeneralError("An unexpected error occurred. Please try again.");
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

            // Validate username
            if (string.IsNullOrEmpty(username))
            {
                ShowError(textViewUsernameError, GetString(Resource.String.signup_error_empty_fields));
                isValid = false;
            }
            else if (username.Length < 3)
            {
                ShowError(textViewUsernameError, GetString(Resource.String.signup_error_username_length));
                isValid = false;
            }

            // Validate email
            if (string.IsNullOrEmpty(email))
            {
                ShowError(textViewEmailError, GetString(Resource.String.signup_error_empty_fields));
                isValid = false;
            }
            else if (!IsValidEmail(email))
            {
                ShowError(textViewEmailError, GetString(Resource.String.signup_error_invalid_email));
                isValid = false;
            }

            // Validate password
            if (string.IsNullOrEmpty(password))
            {
                ShowError(textViewPasswordError, GetString(Resource.String.signup_error_empty_fields));
                isValid = false;
            }
            else if (password.Length < 8)
            {
                ShowError(textViewPasswordError, GetString(Resource.String.signup_error_password_length));
                isValid = false;
            }

            // Validate confirm password
            if (string.IsNullOrEmpty(confirmPassword))
            {
                ShowError(textViewConfirmPasswordError, GetString(Resource.String.signup_error_empty_fields));
                isValid = false;
            }
            else if (password != confirmPassword)
            {
                ShowError(textViewConfirmPasswordError, GetString(Resource.String.signup_error_password_mismatch));
                isValid = false;
            }

            return isValid;
        }

        private static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private void ShowError(TextView errorView, string message)
        {
            errorView.Text = message;
            errorView.Visibility = ViewStates.Visible;
        }

        private void ShowGeneralError(string message)
        {
            textViewGeneralError.Text = message;
            textViewGeneralError.Visibility = ViewStates.Visible;
        }

        private void ClearErrors()
        {
            textViewUsernameError.Visibility = ViewStates.Gone;
            textViewEmailError.Visibility = ViewStates.Gone;
            textViewPasswordError.Visibility = ViewStates.Gone;
            textViewConfirmPasswordError.Visibility = ViewStates.Gone;
            textViewGeneralError.Visibility = ViewStates.Gone;
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
