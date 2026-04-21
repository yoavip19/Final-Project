using Android.App;
using Android.Runtime;

namespace SecurioClient
{
    // Custom Application class required by Android.
    // Password monitoring is handled by PasswordMonitorService (foreground service).
    [Application]
    public class SecurioClientApplication : Application
    {
        public SecurioClientApplication(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer) { }
    }
}
