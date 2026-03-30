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
        /// <summary>
        /// Checks absolute format requirements for the master password.
        /// Returns the first error message found, or null if all checks pass.
        /// For the async HIBP check, call PasswordStrengthHelper.IsCommonPasswordAsync separately.
        /// </summary>
        public static string GetPasswordError(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "Password cannot be empty";

            if (password.Length < 10)
                return "Password must be at least 10 characters";

            if (!Regex.IsMatch(password, "[a-z]"))
                return "Password must contain at least one lowercase letter";

            if (!Regex.IsMatch(password, "[A-Z]"))
                return "Password must contain at least one uppercase letter";

            if (!Regex.IsMatch(password, "\\d"))
                return "Password must contain at least one digit";

            if (!Regex.IsMatch(password, "[!@#$%^&*()_\\-+=\\[{\\]};:<>|./?]"))
                return "Password must contain at least one special character";

            return null; // All checks passed
        }

        /// <summary>
        /// Synchronous password check — format requirements only.
        /// Does NOT include the HIBP check.
        /// </summary>
        public static bool IsStrongPassword(string password)
        {
            return GetPasswordError(password) == null;
        }
    }
}