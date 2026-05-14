using Xamarin.Essentials;

namespace SecurioClient.Helpers
{
    /// <summary>Manages the user's in-app notification enabled/disabled preference.</summary>
    public static class NotificationPreferenceHelper
    {
        private const string KeyNotificationsEnabled = "notifications_enabled";

        /// <summary>Returns true when the user has not disabled in-app notifications (default: enabled).</summary>
        public static bool IsEnabled
            => Preferences.Get(KeyNotificationsEnabled, defaultValue: true);

        /// <summary>Persists the user's notification preference.</summary>
        public static void SetEnabled(bool enabled)
            => Preferences.Set(KeyNotificationsEnabled, enabled);
    }
}
