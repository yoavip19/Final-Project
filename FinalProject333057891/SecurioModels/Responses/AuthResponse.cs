using System;

namespace SecurioModels.Responses
{
    // AuthResponse - Holds session data (Token, ID, Username) returned after successful login or signup.
    public class AuthData
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Token { get; set; }
    }
}
