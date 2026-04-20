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
using System;
using System.Threading.Tasks;

namespace SecurioClient
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class LoginActivity : AppCompatActivity
    {
        private TextInputEditText editTextEmail;
        private TextInputEditText editTextPassword;
        private MaterialButton buttonLogin;
        private TextView textViewEmailError;
        private TextView textViewPasswordError;
        private TextView textViewGeneralError;
        private TextView textViewSignupLink;
        private ProgressBar progressBarLogin;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_login);

            InitializeViews();
            SetupEventHandlers();
        }

        private void InitializeViews()
        {
            editTextEmail = FindViewById<TextInputEditText>(Resource.Id.editTextLoginEmail);
            editTextPassword = FindViewById<TextInputEditText>(Resource.Id.editTextLoginPassword);
            buttonLogin = FindViewById<MaterialButton>(Resource.Id.buttonLogin);
            textViewEmailError = FindViewById<TextView>(Resource.Id.textViewLoginEmailError);
            textViewPasswordError = FindViewById<TextView>(Resource.Id.textViewLoginPasswordError);
            textViewGeneralError = FindViewById<TextView>(Resource.Id.textViewLoginGeneralError);
            textViewSignupLink = FindViewById<TextView>(Resource.Id.textViewSignupLink);
            progressBarLogin = FindViewById<ProgressBar>(Resource.Id.progressBarLogin);
        }

        private void SetupEventHandlers()
        {
            buttonLogin.Click += async (sender, e) => await OnLoginClicked();

            textViewSignupLink.Click += (sender, e) =>
            {
                var intent = new Android.Content.Intent(this, typeof(SignupActivity));
                StartActivity(intent);
                Finish();
            };

            // Real-time validation feedback
            editTextEmail.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                var result = ValidationHelper.ValidateEmail(editTextEmail.Text?.Trim());
                if (!result.IsValid)
                    ShowError(textViewEmailError, result.ErrorMessage);
                else
                    HideError(textViewEmailError);
            }));

            editTextPassword.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                if (string.IsNullOrEmpty(editTextPassword.Text))
                    ShowError(textViewPasswordError, GetString(Resource.String.login_error_password_required));
                else
                    HideError(textViewPasswordError);
            }));
        }

        private async Task OnLoginClicked()
        {
            ClearErrors();

            string email = editTextEmail.Text?.Trim();
            string password = editTextPassword.Text;

            if (!ValidateInputs(email, password))
                return;

            SetLoadingState(true);

            try
            {
                var authService = new AuthService();
                var result = await authService.LoginAsync(email, password);

                if (result.Success)
                {
                    var intent = new Android.Content.Intent(this, typeof(VaultActivity));
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
                ShowGeneralError(GetString(Resource.String.login_error_sign_in_failed));
                System.Diagnostics.Debug.WriteLine($"[LOGIN ERROR] {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private bool ValidateInputs(string email, string password)
        {
            bool isValid = true;

            var emailResult = ValidationHelper.ValidateEmail(email);
            if (!emailResult.IsValid)
            {
                ShowError(textViewEmailError, emailResult.ErrorMessage);
                isValid = false;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError(textViewPasswordError, GetString(Resource.String.login_error_password_required));
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
            HideError(textViewEmailError);
            HideError(textViewPasswordError);
            HideError(textViewGeneralError);
        }

        private void SetLoadingState(bool isLoading)
        {
            progressBarLogin.Visibility = isLoading ? ViewStates.Visible : ViewStates.Gone;
            buttonLogin.Enabled = !isLoading;
            editTextEmail.Enabled = !isLoading;
            editTextPassword.Enabled = !isLoading;
        }

    }
}
