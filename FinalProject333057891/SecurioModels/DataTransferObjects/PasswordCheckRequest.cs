namespace SecurioModels.DataTransferObjects
{
    // Request body for the PasswordCheck endpoint.
    // No JWT is attached because the call originates from the background service,
    // which may run when the user is not actively logged in.
    public class PasswordCheckRequest
    {
        public int UserId { get; set; }
    }
}
