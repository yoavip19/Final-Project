using AndroidX.Fragment.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject333057891
{
    public class SignedMenuFragment : Fragment
    {
        public ISharedPreferences sp;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Inflate the menu layout
            View view = inflater.Inflate(Resource.Layout.signed_menu_fragment_layout, container, false);

            // Get buttons
            Button btnHomepage = view.FindViewById<Button>(Resource.Id.btnMenuHomepage);
            Button btnPasswordList = view.FindViewById<Button>(Resource.Id.btnMenuPasswordList);
            Button btnLogout = view.FindViewById<Button>(Resource.Id.btnMenuLogout);

            sp = Activity.GetSharedPreferences("details", FileCreationMode.Private);

            btnHomepage.Click += (s, e) => {

                Activity.StartActivity(new Intent(Activity, typeof(HomepageActivity)));
            };
            btnPasswordList.Click += (s, e) =>
            {
                Activity.StartActivity(new Intent(Activity, typeof(PasswordListActivity)));
            };
            btnLogout.Click += (s, e) =>
            {
                RemoveSharedPreference();
                Activity.StartActivity(new Intent(Activity, typeof(MainActivity)));
            };

            return view;
        }
        private void RemoveSharedPreference()
        {
            var editor = sp.Edit();
            editor.Remove("Username");
            editor.Remove("Email");
            editor.Remove("Phone");
            editor.Remove("MasterPassword");
            editor.Commit();
        }
    }
}