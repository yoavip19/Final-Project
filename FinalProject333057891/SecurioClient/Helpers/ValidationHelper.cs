using System.Text.RegularExpressions;

namespace SecurioClient.Helpers
{
    // Classifies how strong a password is.
    public enum PasswordStrength
    {
        Weak,
        Fair,
        Strong,
        VeryStrong
    }

    // Carries the result of a single field validation.
    public sealed class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        private ValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Ok() => new ValidationResult(true, null);
        public static ValidationResult Fail(string message) => new ValidationResult(false, message);
    }

    // Industry-standard field validation for the Securio signup / login forms.
    // Rules follow OWASP and NIST SP 800-63B guidance.
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
        // Password strength meter
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns a <see cref="PasswordStrength"/> rating based on how many
        /// complexity criteria the password satisfies.
        ///
        /// Scoring:
        ///   Criteria (each scores 1 point):
        ///     1. length >= 8
        ///     2. length >= 12
        ///     3. contains uppercase
        ///     4. contains lowercase
        ///     5. contains digit
        ///     6. contains special character
        ///
        ///   0-2 points  → Weak
        ///   3-4 points  → Fair
        ///   5   points  → Strong
        ///   6   points  → Very Strong
        /// </summary>
        public static PasswordStrength GetPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return PasswordStrength.Weak;

            int score = 0;

            if (password.Length >= 8) score++;
            if (password.Length >= 12) score++;
            if (HasUppercase.IsMatch(password)) score++;
            if (HasLowercase.IsMatch(password)) score++;
            if (HasDigit.IsMatch(password)) score++;
            if (HasSpecial.IsMatch(password)) score++;

            if (score <= 2) return PasswordStrength.Weak;
            if (score <= 4) return PasswordStrength.Fair;
            if (score == 5) return PasswordStrength.Strong;
            return PasswordStrength.VeryStrong;
        }
    }
}
