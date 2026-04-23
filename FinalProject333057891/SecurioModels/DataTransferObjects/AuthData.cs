using System;

namespace SecurioModels.DataTransferObjects
{
    /// <summary>Holds session data returned after successful login or signup.</summary>
    public class AuthData
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Token { get; set; }
    }
}
