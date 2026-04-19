namespace SecurioModels.DataTransferObjects
{
    // Carries the results of the periodic password-health poll performed by the server.
    // Returned to the client so it can decide which local notifications to fire.
    public class PasswordCheckResult
    {
        public int BreachedCount { get; set; }
        public int OldCount { get; set; }
        public bool MasterPasswordOld { get; set; }
    }
}
