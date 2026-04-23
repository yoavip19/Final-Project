using System;
using System.Collections.Generic;
using System.Text;

namespace SecurioModels.DataTransferObjects
{

    /// <summary>Carries the cryptographic salts required for a client to perform local hashing.</summary>
    public class SaltData
    {
        public string AuthSalt { get; set; }
        public string EncryptionSalt { get; set; }
    }
}
