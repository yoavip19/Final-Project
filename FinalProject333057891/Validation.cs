using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SQLite;
using System.Net.Http;
using System.Threading.Tasks;

namespace FinalProject333057891
{
    public static class Validation
    {
        private static readonly HttpClient client = new HttpClient();
        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;

            var pattern = @"^[A-Za-z][A-Za-z0-9_]{2,19}$";
            return Regex.IsMatch(username, pattern);
        }
        public static bool IsUniqueUsername(Context context, string username)
        {
            // Check username uniqueness

            SQLiteConnection dbCommand = Helper.GetDBCommand(context);
            User checkUsername = dbCommand.Find<User>(username);
            if (checkUsername != null)
            {
                return false;
            }
            return true;
        }
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }
        public static bool IsUniqueEmail(Context context, string email)
        {
            // Check email uniqueness
            SQLiteConnection dbCommand = Helper.GetDBCommand(context);
            var checkEmail = dbCommand.Query<User>("SELECT * FROM Users WHERE Email = ?", email);
            if (checkEmail.Count > 0)
            {
                return false;
            }
            return true;
        }
        public static bool IsValidPhone(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var pattern = @"[0-9]{7}";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }
        public static bool IsUniquePhone(Context context, string phone)
        {
            // Check phone uniqueness
            SQLiteConnection dbCommand = Helper.GetDBCommand(context);
            var checkPhone = dbCommand.Query<User>("SELECT * FROM Users WHERE Phone = ?", phone);
            if (checkPhone.Count > 0)
            {
                return false;
            }
            return true;
        }
        //Using the Pwned Passwords API to check if the password has been exposed in a data breach
        public static async Task<bool> IsPasswordCommonAsync(Context context, string password)
        {
            // SHA-1 hash the password
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
            string fullHash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

            string prefix = fullHash.Substring(0, 5);
            string suffix = fullHash.Substring(5);

            try
            {
                string url = $"https://api.pwnedpasswords.com/range/{prefix}";
                string response = await client.GetStringAsync(url);

                // Each line is "HASHSUFFIX:COUNT"
                foreach (string line in response.Split('\n'))
                {
                    string[] parts = line.Split(':');
                    if (parts[0].Trim().Equals(suffix, StringComparison.OrdinalIgnoreCase))
                        return true; // Password found in breach database
                }
            }
            catch
            {
                Toast.MakeText(context, "Error checking password against breach database", ToastLength.Short).Show();
                return false; // If API fails, don't block the user
            }

            return false;
        }
        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;

            bool hasLower = Regex.IsMatch(password, "[a-z]");
            bool hasUpper = Regex.IsMatch(password, "[A-Z]");
            bool hasDigit = Regex.IsMatch(password, "\\d");
            bool hasSpecial = Regex.IsMatch(password, "[!@#$%^&*()_\\-+=\\[{\\]};:<>|./?]");
            bool longEnough = password.Length >= 8;

            return hasLower && hasUpper && hasDigit && hasSpecial && longEnough;
        }

    }
}