using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProject333057891
{
    [Activity(Label = "PasswordListActivity")]
    public class PasswordListActivity : BaseActivity
    {
        #region Properties
        Button btnAddPasswordToList;

        AndroidX.Fragment.App.Fragment flPasswordListMenuContainer;
        #endregion
        protected override async void OnCreate(Bundle savedInstanceState) //REMOVE ASYNC LATER
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.password_list_layout);
            // Create your application here

            #region FindViewById
            btnAddPasswordToList = FindViewById<Button>(Resource.Id.btnAddPasswordToList);

            SignedMenuFragment fragment = new SignedMenuFragment();
            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.flPasswordListMenuContainer, fragment).Commit();
            #endregion

            btnAddPasswordToList.Click += BtnAddPasswordToList_Click;

            //TEMPORARY
            var popularApps = new List<string>
            {
                "com.whatsapp",                // WhatsApp
                "com.instagram.android",       // Instagram
                "com.facebook.katana",         // Facebook
                "com.zhiliaoapp.musically",    // TikTok
                "com.snapchat.android",        // Snapchat
                "org.telegram.messenger",      // Telegram
                "com.spotify.music",           // Spotify
                "com.netflix.mediaclient",     // Netflix
                "com.google.android.youtube",  // YouTube
                "com.twitter.android",         // Twitter / X
                "com.waze",                    // Waze
                "com.google.android.gm",       // Gmail
                "com.amazon.mShop.android.shopping", // Amazon Shopping
                "com.discord",                 // Discord
                "com.pinterest",               // Pinterest
                "com.linkedin.android",        // LinkedIn
                "com.ubercab",                 // Uber
                "com.paypal.android.p2pmobile",// PayPal
                "com.microsoft.teams",         // Microsoft Teams
                "zoom.us.google"               // Zoom
            };

            // Loop through the list and add them one by one
            foreach (var packageId in popularApps)
            {
                await AddAppSafeAsync(packageId);
            }
        }

        private void BtnAddPasswordToList_Click(object sender, EventArgs e)
        {
            var dialog = new AddPasswordDialog();
            dialog.Show(SupportFragmentManager, "add_password");
        }


        //TEMPORARY
        private async Task AddAppSafeAsync(string packageId)
        {
            try
            {
                SQLiteConnection dbCommand = Helper.GetDBCommand(this);
                // 1. Fetch from API
                var app = await GooglePlayAPI.GetAppMetadataAsync(packageId);

                // 2. Validate
                if (app == null) return;

                // 3. Insert if not exists
                if (dbCommand.Find<Application>(app.PackageID) == null)
                {
                    dbCommand.Insert(app);
                }
            }
            catch
            {
                return;
            }
        }
    }
}