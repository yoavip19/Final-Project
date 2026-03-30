using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FinalProject333057891
{
	public static class PasswordStrengthHelper
	{
        private static readonly HttpClient client = new HttpClient();
        //Using the Pwned Passwords API to check if the password has been exposed in a data breach
        public static async Task<bool> IsCommonPasswordAsync(Context context, string password)
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
    }
}