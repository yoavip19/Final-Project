using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject333057891
{
    public class UnsignedMenuFragment : Fragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Inflate the menu layout
            View view = inflater.Inflate(Resource.Layout.unsigned_menu_fragment_layout, container, false);

            // Get buttons
            Button btnEntry = view.FindViewById<Button>(Resource.Id.btnMenuEntry);
            Button btnSignup = view.FindViewById<Button>(Resource.Id.btnMenuSignup);
            Button btnLogin = view.FindViewById<Button>(Resource.Id.btnMenuLogin);

            // Example click handling
            btnEntry.Click += (s, e) => {
                // Navigate to EntryActivity
                Activity.StartActivity(new Intent(Activity, typeof(MainActivity)));
            };
            btnSignup.Click += (s, e) =>
            {
                Activity.StartActivity(new Intent(Activity, typeof(SignupActivity)));
            };
            btnLogin.Click += (s, e) =>
            {
                Activity.StartActivity(new Intent(Activity, typeof(LoginActivity)));
            };

            return view;
        }
    }
}