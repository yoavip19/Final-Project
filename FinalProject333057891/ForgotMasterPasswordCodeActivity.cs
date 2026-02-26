using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using System;
using System.Threading.Tasks;

namespace FinalProject333057891
{
    [Activity(Label = "ForgotMasterPasswordCodeActivity")]
    public class ForgotMasterPasswordCodeActivity : ForgotMasterPasswordActivity
    {
        #region Properties
        public string updatedUsername;
        public string forgotMasterPasswordCode;
        public string userEmail;

        private const int CODE_EXPIRY_SECONDS = 120; // 2 minutes
        private bool isCodeExpired = false;
        private bool isTimerRunning = false;

        private System.Threading.CancellationTokenSource _timerCts;
        #endregion

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            tvForgotMasterPasswordField.Text = "Code";

            etForgotMasterPassword.InputType = InputTypes.TextVariationNormal;
            etForgotMasterPassword.Hint = "Enter 8-character code";

            tvForgotMasterPasswordError.Text = "*Incorrect or expired code";

            btnForgotMasterPassword.Text = "Submit";

            updatedUsername = Intent.GetStringExtra("UsernameToUpdate");
            forgotMasterPasswordCode = Intent.GetStringExtra("EmailCode");
            userEmail = Intent.GetStringExtra("UserEmail");

            tvCodeTimer.Visibility = ViewStates.Visible;
            btnResendCode.Visibility = ViewStates.Visible;

            btnForgotMasterPassword.Click += BtnForgotMasterPassword_SubmitCode;
            btnResendCode.Click += BtnResendCode_Click;

            StartCountdown();
        }
        private void StartCountdown()
        {
            isCodeExpired = false;
            // Cancel any previous timer (e.g. after resend)
            _timerCts?.Cancel();
            _timerCts = new System.Threading.CancellationTokenSource();
            var token = _timerCts.Token;

            Task.Run(async () =>
            {
                for (int secondsLeft = CODE_EXPIRY_SECONDS; secondsLeft >= 0; secondsLeft--)
                {
                    if (token.IsCancellationRequested) return;

                    long minutes = secondsLeft / 60;
                    long seconds = secondsLeft % 60;

                    RunOnUiThread(() =>
                    {
                        tvCodeTimer.Text = $"Code expires in: {minutes}:{seconds:D2}";
                    });

                    if (secondsLeft == 0) break;
                    await Task.Delay(1000, token).ContinueWith(_ => { }); // swallow cancellation
                }

                if (token.IsCancellationRequested) return;

                isCodeExpired = true;
                RunOnUiThread(() =>
                {
                    tvCodeTimer.Text = "Code expired.";
                    tvCodeTimer.SetTextColor(GetColorStateList(Resource.Color.errorColor));
                    btnResendCode.Visibility = ViewStates.Visible;
                });
            }, token);
        }

        private void BtnForgotMasterPassword_SubmitCode(object sender, EventArgs e)
        {
            if (isCodeExpired)
            {
                tvForgotMasterPasswordError.Text = "*Code has expired. Please request a new one.";
                tvForgotMasterPasswordError.Visibility = ViewStates.Visible;
                return;
            }

            if (forgotMasterPasswordCode == etForgotMasterPassword.Text.Trim())
            {
                Intent intent = new Intent(this, typeof(UpdateMasterPasswordActivity));
                intent.PutExtra("UsernameToUpdate", updatedUsername);
                StartActivity(intent);
            }
            else
            {
                tvForgotMasterPasswordError.Visibility = ViewStates.Visible;
            }
        }

        private async void BtnResendCode_Click(object sender, EventArgs e)
        {
            btnResendCode.Enabled = false;
            btnResendCode.Text = "Sending...";

            forgotMasterPasswordCode = GenerateEmailCode();
            string messageBody = $"Hello, {updatedUsername}! Your new code is: {forgotMasterPasswordCode}";

            await EmailHelper.SendEmailAsync(userEmail, messageBody);

            etForgotMasterPassword.Text = "";
            tvForgotMasterPasswordError.Visibility = ViewStates.Gone;

            btnResendCode.Enabled = true;
            btnResendCode.Text = "Resend Code";

            StartCountdown();
        }
    }
}