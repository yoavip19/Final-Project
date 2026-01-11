using AndroidX.Fragment.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AndroidX.AppCompat.App;
using SQLite;

namespace FinalProject333057891
{
    public class BaseActivity : AppCompatActivity
    {
        public static ISharedPreferences sp;
        public static SQLiteConnection dbCommand;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            sp = GetSharedPreferences("details", FileCreationMode.Private);
        }
    }
}