using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;

namespace SecurioClient.Helpers
{
    /// <summary>Utility for battery-optimisation and manufacturer-specific autostart onboarding.</summary>
    public static class PowerManagerHelper
    {
        /// <summary>Returns true when this app is already exempt from battery optimisations.</summary>
        public static bool IsIgnoringBatteryOptimizations(Context context)
        {
            var pm = context.GetSystemService(Context.PowerService) as PowerManager;
            return pm?.IsIgnoringBatteryOptimizations(context.PackageName) ?? true;
        }

        /// <summary>
        /// Fires the system dialog that asks the user to exempt this app from battery optimisations.
        /// The calling activity should handle the result in OnActivityResult.
        /// </summary>
        public static void RequestIgnoreBatteryOptimizations(Activity activity, int requestCode)
        {
            var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse("package:" + activity.PackageName));
            activity.StartActivityForResult(intent, requestCode);
        }

        /// <summary>
        /// Returns an Intent for the OEM-specific Autostart / Protected-Apps settings screen,
        /// or null when the device brand has no known deep-link (stock Android needs no extra step).
        /// The caller should verify the Intent is resolvable before starting it.
        /// </summary>
        public static Intent GetManufacturerAutostartIntent()
        {
            string brand = (Build.Brand ?? "").ToLowerInvariant();

            // Xiaomi / MIUI (Redmi, POCO)
            if (brand.Contains("xiaomi") || brand.Contains("redmi") || brand.Contains("poco"))
            {
                var intent = new Intent();
                intent.SetComponent(new ComponentName(
                    "com.miui.securitycenter",
                    "com.miui.permcenter.autostart.AutoStartManagementActivity"));
                return intent;
            }

            // Huawei / Honor
            if (brand.Contains("huawei") || brand.Contains("honor"))
            {
                var intent = new Intent();
                intent.SetComponent(new ComponentName(
                    "com.huawei.systemmanager",
                    "com.huawei.systemmanager.startupmgr.ui.StartupNormalAppListActivity"));
                return intent;
            }

            // Oppo / Realme / OnePlus (ColorOS)
            if (brand.Contains("oppo") || brand.Contains("realme") || brand.Contains("oneplus"))
            {
                var intent = new Intent();
                intent.SetComponent(new ComponentName(
                    "com.coloros.safecenter",
                    "com.coloros.privacypermissionsentry.PermissionTopActivity"));
                return intent;
            }

            // Vivo / iQOO (FuntouchOS)
            if (brand.Contains("vivo"))
            {
                var intent = new Intent();
                intent.SetComponent(new ComponentName(
                    "com.vivo.permissionmanager",
                    "com.vivo.permissionmanager.activity.BgStartUpManagerActivity"));
                return intent;
            }

            // Samsung (One UI)
            if (brand.Contains("samsung"))
            {
                var intent = new Intent();
                intent.SetComponent(new ComponentName(
                    "com.samsung.android.lool",
                    "com.samsung.android.sm.battery.ui.BatteryActivity"));
                return intent;
            }

            return null;
        }
    }
}
