using Android.Content;
using Android.Util;
using SecurioClient.Activities;
using System.Threading;
using System.Threading.Tasks;

namespace SecurioClient.Helpers
{
    /// <summary>
    /// Handles a server 401 Unauthorized response by clearing the local session and
    /// routing the user back to <see cref="LoginActivity"/>.
    /// An Interlocked flag prevents multiple in-flight requests from each triggering
    /// a separate redirect.
    /// </summary>
    public static class SessionExpiredHandler
    {
        private static int _handling = 0;

        /// <summary>
        /// Clears the session and navigates to <see cref="LoginActivity"/>.
        /// Safe to call from any thread; uses <see cref="Android.App.Application.Context"/>
        /// for the Intent so no Activity reference is needed.
        /// </summary>
        public static async Task OnSessionExpiredAsync()
        {
            // Only the first caller proceeds; all others are no-ops.
            if (Interlocked.Exchange(ref _handling, 1) == 1) return;

            try
            {
                Log.Debug(TestConfig.LogTag, "SessionExpiredHandler — JWT expired, redirecting to login.");

                SessionHelper.EndSession();
                await StorageHelper.ClearSessionAsync();

                var context = Android.App.Application.Context;
                var intent = new Intent(context, typeof(LoginActivity));
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
                context.StartActivity(intent);
            }
            finally
            {
                Interlocked.Exchange(ref _handling, 0);
            }
        }
    }
}
