using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;

namespace ConsoleFunctionsCheck
{
    internal static class MailSend
    {
        public static async Task SendCodeAsync()
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("My App", "YudBet4IroniA@gmail.com"));
            message.To.Add(new MailboxAddress("", "1000374513@jerschools.org.il"));
            message.Subject = "Password Recovery";

            message.Body = new TextPart(MimeKit.Text.TextFormat.Plain)
            {
                Text = $"Hello, your code is: some code"
            };

            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

                // Gmail now requires App Passwords (not your normal Gmail password!)
                await client.AuthenticateAsync("YudBet4IroniA@gmail.com", "qhip imme dcek jgus");

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
