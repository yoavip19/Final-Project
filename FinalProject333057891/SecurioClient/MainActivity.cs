using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using SecurioClient.Helpers;

namespace SecurioClient
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // Route to the appropriate screen based on session state.
            // If the user has an active in-memory session key they are already
            // authenticated; otherwise send them to the login screen.
            StorageHelper.ClearAll();

            if (!string.IsNullOrEmpty(await StorageHelper.GetJwt()))
            {
                var vaultIntent = new Android.Content.Intent(this, typeof(VaultActivity));
                vaultIntent.SetFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.ClearTask);
                StartActivity(vaultIntent);
                Finish();
            }
            else
            {
                var intent = new Android.Content.Intent(this, typeof(LoginActivity));
                StartActivity(intent);
                Finish();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
