using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Graphics;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FinalProject333057891
{
    /// <summary>
    /// Combined model for displaying password entries with their associated app info
    /// </summary>
    public class PasswordItem
    {
        public string Username { get; set; }
        public string AppPackageID { get; set; }
        public string AppName { get; set; }
        public string AppUsername { get; set; }
        public string IconBase64 { get; set; }
        public string PasswordEncrypted { get; set; }
        public string Salt { get; set; }
        public string InitVector { get; set; }
        public bool IsFavorite { get; set; }

        public PasswordItem(Password password, Application app)
        {
            Username = password.Username;
            AppPackageID = password.AppPackageID;
            AppUsername = password.AppUsername;
            PasswordEncrypted = password.PasswordEncrypted;
            Salt = password.Salt;
            InitVector = password.InitVector;
            IsFavorite = password.IsFavorite;

            AppName = app.AppName;
            IconBase64 = app.IconBase64;
        }

        /// <summary>
        /// Gets the app icon as a Bitmap
        /// </summary>
        public Bitmap GetIconBitmap()
        {
            return Application.Base64ToBitmap(IconBase64);
        }
    }
}