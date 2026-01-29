using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Runtime;
using Java.Lang;

namespace FinalProject333057891
{
    // Autocomplete suggestion
    public class AppSuggestion : Java.Lang.Object
    {
        public string AppName { get; set; } = "";
        public string PackageID { get; set; } = "";
        public string IconBase64 { get; set; } = "";

        public override string ToString()
        {
            return AppName;
        }
    }
}