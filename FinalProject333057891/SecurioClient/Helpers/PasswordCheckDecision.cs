using SecurioModels.DataTransferObjects;
using System.Text;

namespace SecurioClient.Helpers
{
    /// <summary>Pure, platform-independent helper that decides whether the user should be notified after a PasswordCheck server response.</summary>
    public static class PasswordCheckDecision
    {
        /// <summary>Returns true when at least one password-health issue was detected.</summary>
        public static bool ShouldNotify(PasswordCheckResult result)
        {
            if (result == null) return false;
            return result.BreachedCount > 0 || result.OldCount > 0 || result.MasterPasswordOld;
        }

        /// <summary>Builds a human-readable notification body summarising the issues found.</summary>
        public static string BuildMessage(PasswordCheckResult result)
        {
            if (result == null || !ShouldNotify(result))
                return string.Empty;

            var parts = new System.Collections.Generic.List<string>();

            if (result.BreachedCount > 0)
                parts.Add($"{result.BreachedCount} leaked password{(result.BreachedCount == 1 ? "" : "s")}");

            if (result.OldCount > 0)
                parts.Add($"{result.OldCount} old password{(result.OldCount == 1 ? "" : "s")}");

            if (result.MasterPasswordOld)
                parts.Add("master password not changed in 90+ days");

            return "Securio alert: " + string.Join(", ", parts) + ".";
        }
    }
}
