using System;
using System.Collections.Generic;
using System.Text;

namespace SecurioModels
{
    /// <summary>The standard template for every server response, ensuring a success flag and message are always present.</summary>
    public class ServerResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
