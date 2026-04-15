using System;
using System.Collections.Generic;
using System.Text;

namespace SecurioModels
{
    // The standard template for every server response, ensuring a success flag and a descriptive message are always present.
    public class ServerResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
