  public enum PasswordStrength { Weak, Fair, Strong, VeryStrong }

  public static class ValidationHelper
  {
      public static (bool IsValid, string ErrorMessage) ValidateUsername(string username)
          => string.IsNullOrWhiteSpace(username) ? (false, "Username required") : (true, null);

      public static (bool IsValid, string ErrorMessage) ValidateEmail(string email)
          => string.IsNullOrWhiteSpace(email) || !email.Contains("@") ? (false, "Invalid email") : (true, null);

      public static (bool IsValid, string ErrorMessage) ValidatePassword(string password)
          => string.IsNullOrWhiteSpace(password) || password.Length < 6 ? (false, "Password too short") : (true, null);

      public static (bool IsValid, string ErrorMessage) ValidatePasswordsMatch(string p1, string p2)
          => p1 == p2 ? (true, null) : (false, "Passwords do not match");

      public static PasswordStrength GetPasswordStrength(string password)
      {
          if (string.IsNullOrEmpty(password)) return PasswordStrength.Weak;
          if (password.Length < 6) return PasswordStrength.Weak;
          if (password.Length < 10) return PasswordStrength.Fair;
          if (password.Length < 14) return PasswordStrength.Strong;
          return PasswordStrength.VeryStrong;
      }
  }
  