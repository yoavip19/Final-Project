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
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Inflate the menu layout
            View view = inflater.Inflate(Resource.Layout.signed_menu_fragment_layout, container, false);

            // Get buttons
            Button btnHomepage = view.FindViewById<Button>(Resource.Id.btnMenuHomepage);
            Button btnPasswordList = view.FindViewById<Button>(Resource.Id.btnMenuPasswordList);

            // Example click handling
            btnHomepage.Click += (s, e) => {
                // Navigate to EntryActivity
                Activity.StartActivity(new Intent(Activity, typeof(HomepageActivity)));
            };
            btnPasswordList.Click += (s, e) =>
            {
                Activity.StartActivity(new Intent(Activity, typeof(PasswordListActivity)));
            };

            return view;
        }
    }
}