using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;

namespace FinalProject333057891
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : BaseActivity
    {
        #region Constants
        public const string Key = "7607fe5ec0ffb1d79744ef9d6e87b5b372360197030129292e46937a979f4aa2";
        public const string MailFrom = "yudbet4ironia@gmail.com";
        public const string AppPassword = "qhip imme dcek jgus";
        #endregion
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            UnsignedMenuFragment fragment = new UnsignedMenuFragment();
            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.flMainMenuContainer, fragment).Commit();

            Helper.Initialize(this);

        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}