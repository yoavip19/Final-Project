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
    [Activity(Label = "ForgotMasterPasswordEmailActivity")]
    public class ForgotMasterPasswordEmailActivity : ForgotMasterPasswordActivity
    {

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            tvForgotMasterPasswordField.Text = "Email";

            etForgotMasterPassword.InputType = InputTypes.TextVariationEmailAddress;
            etForgotMasterPassword.Hint = "Enter email";

            tvForgotMasterPasswordError.Text = "*Incorrect email";

            btnForgotMasterPassword.Text = "Send code";

            btnForgotMasterPassword.Click += BtnForgotMasterPassword_Click; ;
        }

        private async void BtnForgotMasterPassword_Click(object sender, EventArgs e)
        {
            //Handles code emailing and inflates code fragment
            User userToRecover = FindUserByEmail(etForgotMasterPassword.Text);
            string emailCode;
            if (userToRecover != null)
            {
                emailCode = GenerateEmailCode();
                string messageBody = $"Hello, {userToRecover.Username}! Your 6-digit code is: {emailCode}";

                await EmailHelper.SendEmailAsync(userToRecover.Email, messageBody);

                Intent intent = new Intent(this, typeof(ForgotMasterPasswordCodeActivity));

                intent.PutExtra("UsernameToUpdate", userToRecover.Username);
                intent.PutExtra("EmailCode", emailCode);
                intent.PutExtra("UserEmail", userToRecover.Email);

                StartActivity(intent);
            }
        }

        private User FindUserByEmail(string email)
        {
            //Gets the user with an email
            try
            {
                string query = "SELECT * FROM Users WHERE Email = ?;";
                dbCommand = Helper.GetDBCommand(this);
                var userList = dbCommand.Query<User>(query, email);
                if (userList.Count > 0)
                {
                    return userList[0];
                }
                else
                {
                    Toast.MakeText(this, "Email does not exist", ToastLength.Short).Show();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Code error:\n" + ex.ToString(), ToastLength.Short).Show();
                return null;
            }
        }
    }
}