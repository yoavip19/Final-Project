using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SecurioClient.Helpers
{
    /// <summary>Classifies how strong a password is across five levels.</summary>
    public enum PasswordStrength
    {
        Weak,       // score 1
        Poor,       // score 2
        Fair,       // score 3
        Strong,     // score 4
        VeryStrong  // score 5
    }

    /// <summary>Carries the result of a single field validation.</summary>
    public sealed class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        /// <summary>Initializes a new instance of ValidationResult.</summary>
        private ValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        /// <summary>Returns a successful validation result.</summary>
        public static ValidationResult Ok() => new ValidationResult(true, null);
        /// <summary>Returns a failed validation result with the specified error message.</summary>
        public static ValidationResult Fail(string message) => new ValidationResult(false, message);
    }

    /// <summary>Industry-standard field validation for the Securio signup and login forms following OWASP and NIST SP 800-63B guidance.</summary>
    public static class ValidationHelper
    {
        // -----------------------------------------------------------------
        // Regex constants
        // -----------------------------------------------------------------

        // Username: starts with a letter; only letters, digits, underscores,
        // or hyphens; 3-30 characters total.
        private static readonly Regex UsernameRegex =
            new Regex(@"^[a-zA-Z][a-zA-Z0-9_\-]{2,29}$", RegexOptions.Compiled);

        // Email: RFC 5321-compatible loose check that covers real-world addresses.
        private static readonly Regex EmailRegex =
            new Regex(@"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Password component detectors
        private static readonly Regex HasUppercase = new Regex(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex HasLowercase = new Regex(@"[a-z]", RegexOptions.Compiled);
        private static readonly Regex HasDigit = new Regex(@"\d", RegexOptions.Compiled);

        // Accepted special characters: ! " # $ % & ' ( ) * + , - . / : ; < = > ? @ [ \ ] ^ _ ` { | } ~
        // These cover the full set of ASCII printable non-alphanumeric characters (OWASP recommended).
        private static readonly Regex HasSpecial =
            new Regex(@"[!@#$%^&*()_+\-=\[\]{}|;':"",./<>?\\`~]", RegexOptions.Compiled);

        // Character pools used by GenerateStrongPassword
        private const string UpperPool   = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowerPool   = "abcdefghijklmnopqrstuvwxyz";
        private const string DigitPool   = "0123456789";
        private const string SpecialPool = "!@#$%^&*()-_=+[]{}|;:,.<>?";

        // -----------------------------------------------------------------
        // Public validators
        // -----------------------------------------------------------------

        /// <summary>Validates a username against Securio naming rules.</summary>
        public static ValidationResult ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return ValidationResult.Fail("Username is required.");

            if (username.Length < 3)
                return ValidationResult.Fail("Username must be at least 3 characters.");

            if (username.Length > 30)
                return ValidationResult.Fail("Username must be 30 characters or fewer.");

            if (!char.IsLetter(username[0]))
                return ValidationResult.Fail("Username must start with a letter.");

            if (!UsernameRegex.IsMatch(username))
                return ValidationResult.Fail("Username may only contain letters, digits, underscores, or hyphens.");

            return ValidationResult.Ok();
        }

        /// <summary>Validates an email address format.</summary>
        public static ValidationResult ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ValidationResult.Fail("Email address is required.");

            if (!EmailRegex.IsMatch(email))
                return ValidationResult.Fail("Please enter a valid email address.");

            return ValidationResult.Ok();
        }

        /// <summary>
        /// Validates password strength.  Returns an error for the first unmet
        /// requirement so the message is actionable rather than overwhelming.
        /// </summary>
        public static ValidationResult ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return ValidationResult.Fail("Password is required.");

            if (password.Length < 8)
                return ValidationResult.Fail("Password must be at least 8 characters.");

            if (!HasUppercase.IsMatch(password))
                return ValidationResult.Fail("Password must include at least one uppercase letter.");

            if (!HasLowercase.IsMatch(password))
                return ValidationResult.Fail("Password must include at least one lowercase letter.");

            if (!HasDigit.IsMatch(password))
                return ValidationResult.Fail("Password must include at least one digit.");

            if (!HasSpecial.IsMatch(password))
                return ValidationResult.Fail("Password must include at least one special character (e.g. !@#$).");

            return ValidationResult.Ok();
        }

        /// <summary>Checks that the two password fields are identical.</summary>
        public static ValidationResult ValidatePasswordsMatch(string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(confirmPassword))
                return ValidationResult.Fail("Please confirm your password.");

            if (password != confirmPassword)
                return ValidationResult.Fail("Passwords do not match.");

            return ValidationResult.Ok();
        }

        // -----------------------------------------------------------------
        // Password strength meter (5-criterion cumulative score)
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns a score from 0 to 5 reflecting how many of the five
        /// complexity criteria the password satisfies:
        ///   1. Length >= 8
        ///   2. Contains a lowercase letter
        ///   3. Contains an uppercase letter
        ///   4. Contains a digit
        ///   5. Contains a special character
        /// </summary>
        public static int GetPasswordScore(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;

            int score = 0;
            if (password.Length >= 8)             score++;
            if (HasLowercase.IsMatch(password))   score++;
            if (HasUppercase.IsMatch(password))   score++;
            if (HasDigit.IsMatch(password))       score++;
            if (HasSpecial.IsMatch(password))     score++;

            return score;
        }

        /// <summary>Maps a cumulative score (0-5) to a PasswordStrength level.</summary>
        public static PasswordStrength GetPasswordStrength(string password)
        {
            int score = GetPasswordScore(password);
            if (score <= 1) return PasswordStrength.Weak;
            if (score == 2) return PasswordStrength.Poor;
            if (score == 3) return PasswordStrength.Fair;
            if (score == 4) return PasswordStrength.Strong;
            return PasswordStrength.VeryStrong;
        }

        /// <summary>
        /// Returns a short, constructive hint listing which criteria are still unmet,
        /// or a success message when all five criteria are satisfied.
        /// </summary>
        public static string GetMissingCriteriaHint(string password)
        {
            if (string.IsNullOrEmpty(password)) return null;

            var missing = new List<string>();
            if (password.Length < 8)             missing.Add("8+ chars");
            if (!HasLowercase.IsMatch(password)) missing.Add("lowercase");
            if (!HasUppercase.IsMatch(password)) missing.Add("uppercase");
            if (!HasDigit.IsMatch(password))     missing.Add("digit");
            if (!HasSpecial.IsMatch(password))   missing.Add("special char");

            return missing.Count == 0
                ? "All requirements met ✓"
                : "Missing: " + string.Join(", ", missing);
        }

        // -----------------------------------------------------------------
        // Password generator
        // -----------------------------------------------------------------

        /// <summary>Generates a cryptographically random password of the specified length satisfying all five complexity criteria.</summary>
        public static string GenerateStrongPassword(int length = 16)
        {
            if (length < 12) length = 12;

            string allChars = UpperPool + LowerPool + DigitPool + SpecialPool;

            // Start with one guaranteed character from each required category.
            var chars = new List<char>
            {
                UpperPool[SecureRandomIndex(UpperPool.Length)],
                LowerPool[SecureRandomIndex(LowerPool.Length)],
                DigitPool[SecureRandomIndex(DigitPool.Length)],
                SpecialPool[SecureRandomIndex(SpecialPool.Length)]
            };

            // Fill the remainder with random characters from the combined pool.
            for (int i = chars.Count; i < length; i++)
                chars.Add(allChars[SecureRandomIndex(allChars.Length)]);

            // Fisher-Yates shuffle so the guaranteed characters aren't always at the start.
            for (int i = chars.Count - 1; i > 0; i--)
            {
                int j = SecureRandomIndex(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }

        /// <summary>Returns a cryptographically secure random index in [0, max) using rejection sampling to eliminate modulo bias.</summary>
        private static int SecureRandomIndex(int max)
        {
            // Largest multiple of max that fits in a uint, used as the rejection threshold.
            uint limit = uint.MaxValue - (uint.MaxValue % (uint)max);
            byte[] buf = new byte[4];
            uint raw;
            do
            {
                RandomNumberGenerator.Fill(buf);
                raw = BitConverter.ToUInt32(buf, 0);
            }
            while (raw >= limit);

            return (int)(raw % (uint)max);
        }
    }
}
