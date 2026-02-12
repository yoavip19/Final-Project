using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MailKit.Net.Smtp;
using MimeKit;

namespace FinalProject333057891
{
	static class EmailHelper
	{
        #region Constants
        private const string MAIL_FROM = MainActivity.MailFrom;
        private const string APP_PASSWORD = MainActivity.AppPassword;
        #endregion
        public static async Task SendEmailAsync(string email, string messageBody)
        {
            //Emails a message to a user
            if (string.IsNullOrEmpty(email))
            {
                return;
            }
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("My App", MAIL_FROM));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Password Recovery";

            message.Body = new TextPart("plain")
            {
                Text = messageBody
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