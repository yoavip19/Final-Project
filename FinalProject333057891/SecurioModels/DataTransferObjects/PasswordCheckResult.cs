namespace SecurioModels.DataTransferObjects
{
    /// <summary>Carries the password-health summary returned by the PasswordCheck endpoint.</summary>
    public class PasswordCheckResult
    {
        // Number of vault items whose stored password has been found in a data breach.
        public int BreachedCount { get; set; }

        // Number of vault items whose password has not been changed in more than 90 days.
        public int OldCount { get; set; }

        // True when the user's master password itself has not been changed in 90+ days.
        public bool MasterPasswordOld { get; set; }
    }
}
