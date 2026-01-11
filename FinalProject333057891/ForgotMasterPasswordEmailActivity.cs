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
        #region Constants
        private const string MAIL_FROM = "yudbet4ironia@gmail.com";
        private const string APP_PASSWORD = "qhip imme dcek jgus";
        #endregion

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

                await SendCodeAsync(userToRecover, emailCode);

                Intent intent = new Intent(this, typeof(ForgotMasterPasswordCodeActivity));

                intent.PutExtra("UsernameToUpdate", userToRecover.Username);
                intent.PutExtra("EmailCode", emailCode);

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

        private string GenerateEmailCode()
        {
            //Generates 6-digit code
            Random randomNumberGenerator = new Random();
            const int CODE_LENGTH = 6;
            string code = "";
            for (int i = 0; i < CODE_LENGTH; i++)
            {
                code += randomNumberGenerator.Next(10);
            }
            return code;
        }

        private static async Task SendCodeAsync(User userToRecover, string forgotMasterPasswordCode)
        {
            //Emails the username and 6-digit code to a user
            if (userToRecover == null)
            {
                return;
            }
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("My App", MAIL_FROM));
            message.To.Add(new MailboxAddress("", userToRecover.Email));
            message.Subject = "Password Recovery";

            message.Body = new TextPart("plain")
            {
                Text = $"Hello, {userToRecover.Username}! Your 6-digit code is: {forgotMasterPasswordCode}"
            };

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

            // Gmail now requires App Passwords (not your normal Gmail password!)
            await client.AuthenticateAsync(MAIL_FROM, APP_PASSWORD);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}