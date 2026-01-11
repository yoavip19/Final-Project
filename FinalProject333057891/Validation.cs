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

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
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