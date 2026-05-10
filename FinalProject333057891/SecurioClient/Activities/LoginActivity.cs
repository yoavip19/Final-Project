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

namespace SecurioClient.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    /// <summary>Activity that provides the login form and authenticates the user.</summary>
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

        /// <summary>Initializes the activity, inflates the layout, and wires up views and handlers.</summary>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_login);

            InitializeViews();
            SetupEventHandlers();
        }

        /// <summary>Finds and assigns all view references from the layout.</summary>
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

        /// <summary>Wires up click and text-change event handlers for login form controls.</summary>
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
                    FormUiHelper.ShowError(textViewEmailError, result.ErrorMessage);
                else
                    FormUiHelper.HideError(textViewEmailError);
            }));

            editTextPassword.AddTextChangedListener(new SimpleTextWatcher(_ =>
            {
                if (string.IsNullOrEmpty(editTextPassword.Text))
                    FormUiHelper.ShowError(textViewPasswordError, GetString(Resource.String.login_error_password_required));
                else
                    FormUiHelper.HideError(textViewPasswordError);
            }));
        }

        /// <summary>Validates inputs and attempts to log in the user via AuthService.</summary>
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
                    // Clear any stale lock state and arm the inactivity timer for the new session.
                    AppLockManager.Unlock();
                    AppLockManager.ResetAutoLockTimer();

                    MainActivity.StartPasswordMonitor(this);
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
                FormUiHelper.ShowError(textViewGeneralError, GetString(Resource.String.login_error_sign_in_failed));
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>Validates the email and password fields and shows errors for any invalid values.</summary>
        private bool ValidateInputs(string email, string password)
        {
            bool isValid = true;

            var emailResult = ValidationHelper.ValidateEmail(email);
            if (!emailResult.IsValid)
            {
                FormUiHelper.ShowError(textViewEmailError, emailResult.ErrorMessage);
                isValid = false;
            }

            if (string.IsNullOrEmpty(password))
            {
                FormUiHelper.ShowError(textViewPasswordError, GetString(Resource.String.login_error_password_required));
                isValid = false;
            }

            return isValid;
        }

        /// <summary>Hides all field-level and general error messages.</summary>
        private void ClearErrors()
        {
            FormUiHelper.HideError(textViewEmailError);
            FormUiHelper.HideError(textViewPasswordError);
            FormUiHelper.HideError(textViewGeneralError);
        }

        /// <summary>Toggles the loading state, showing the progress bar and disabling form controls.</summary>
        private void SetLoadingState(bool isLoading)
        {
            progressBarLogin.Visibility = isLoading ? ViewStates.Visible : ViewStates.Gone;
            buttonLogin.Enabled = !isLoading;
            editTextEmail.Enabled = !isLoading;
            editTextPassword.Enabled = !isLoading;
        }

    }
}
