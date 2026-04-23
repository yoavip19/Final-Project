using Android.App;
using Android.Runtime;
using System;

namespace SecurioClient
{
    /// <summary>Custom Application class that provides Android application-level initialization.</summary>
    [Application]
    public class SecurioClientApplication : Application
    {
        /// <summary>Initializes a new instance of SecurioClientApplication.</summary>
        public SecurioClientApplication(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer) { }
    }
}
