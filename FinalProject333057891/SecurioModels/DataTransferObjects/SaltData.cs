using System;
using System.Collections.Generic;
using System.Text;

namespace SecurioModels.DataTransferObjects
{

    // Carries the cryptographic salts required for a client to perform local hashing.
    public class SaltData
    {
        public string AuthSalt { get; set; }
        public string EncryptionSalt { get; set; }
    }
}
