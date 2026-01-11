using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using MailKit.Net.Smtp;
using MimeKit;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProject333057891
{
    [Activity(Label = "ForgotMasterPasswordCodeActivity")]
    public class ForgotMasterPasswordCodeActivity : ForgotMasterPasswordActivity
    {
        #region Properties
        public string updatedUsername;
        public string forgotMasterPasswordCode;
        #endregion
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            tvForgotMasterPasswordField.Text = "Code";

            etForgotMasterPassword.InputType = InputTypes.NumberVariationNormal;
            etForgotMasterPassword.Hint = "Enter 6-digit code";

            tvForgotMasterPasswordError.Text = "*Incorrect code";

            btnForgotMasterPassword.Text = "Submit";

            updatedUsername = Intent.GetStringExtra("UsernameToUpdate");
            forgotMasterPasswordCode = Intent.GetStringExtra("EmailCode");

            btnForgotMasterPassword.Click += BtnForgotMasterPassword_SubmitCode;
        }

        private void BtnForgotMasterPassword_SubmitCode(object sender, EventArgs e)
        {
            //Checks if the code is correct
            if (forgotMasterPasswordCode == etForgotMasterPassword.Text)
            {
                Intent intent = new Intent(this, typeof(UpdateMasterPasswordActivity));

                intent.PutExtra("UsernameToUpdate", updatedUsername);

                StartActivity(intent);
            }
        }
    }
}