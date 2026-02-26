using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using Android.Text;

namespace FinalProject333057891
{
    public class ViewPasswordDialog : BottomSheetDialogFragment
    {
        // UI Elements
        ImageView ivViewAppIcon;
        TextView tvViewAppName;
        TextView tvViewAppUsername;
        TextView tvViewPassword;
        Button btnCopyPassword;
        Button btnTogglePasswordVisibility;
        LinearLayout llSecurityWarning;

        // Data
        private PasswordItem passwordItem;
        private string decryptedPassword;
        private bool isPasswordVisible = false;

        // Constructor - now accepts the decrypted password directly
        public ViewPasswordDialog(PasswordItem item, string password)
        {
            passwordItem = item;
            decryptedPassword = password;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.view_password_dialog_layout, container, false);

            #region FindViewById
            ivViewAppIcon = view.FindViewById<ImageView>(Resource.Id.ivViewAppIcon);
            tvViewAppName = view.FindViewById<TextView>(Resource.Id.tvViewAppName);
            tvViewAppUsername = view.FindViewById<TextView>(Resource.Id.tvViewAppUsername);
            tvViewPassword = view.FindViewById<TextView>(Resource.Id.tvViewPassword);
            btnCopyPassword = view.FindViewById<Button>(Resource.Id.btnCopyPassword);
            btnTogglePasswordVisibility = view.FindViewById<Button>(Resource.Id.btnTogglePasswordVisibility);
            llSecurityWarning = view.FindViewById<LinearLayout>(Resource.Id.tvSecurityWarning);
            #endregion

            // Populate app info
            PopulateAppInfo();

            // Password is already decrypted and passed in - enable buttons immediately
            btnCopyPassword.Enabled = true;
            btnTogglePasswordVisibility.Enabled = true;

            // Event handlers
            btnCopyPassword.Click += BtnCopyPassword_Click;
            btnTogglePasswordVisibility.Click += BtnTogglePasswordVisibility_Click;

            return view;
        }

        private void PopulateAppInfo()
        {
            // Set app name
            tvViewAppName.Text = passwordItem.AppName;

            // Set app icon
            try
            {
                ivViewAppIcon.SetImageBitmap(passwordItem.GetIconBitmap());
            }
            catch
            {
                ivViewAppIcon.SetImageResource(Android.Resource.Drawable.IcMenuGallery);
            }

            // Set app username
            tvViewAppUsername.Text = $"Username: {passwordItem.AppUsername}";

            // Initially hide password
            tvViewPassword.Text = "••••••••";
        }

        private void BtnTogglePasswordVisibility_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(decryptedPassword))
            {
                Toast.MakeText(Activity, "Password not available", ToastLength.Short).Show();
                return;
            }

            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                tvViewPassword.Text = decryptedPassword;
                btnTogglePasswordVisibility.Text = "Hide";
            }
            else
            {
                tvViewPassword.Text = "••••••••";
                btnTogglePasswordVisibility.Text = "Show";
            }
        }

        private void BtnCopyPassword_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(decryptedPassword))
            {
                Toast.MakeText(Activity, "Password not available", ToastLength.Short).Show();
                return;
            }

            // Copy to clipboard
            Android.Content.ClipboardManager clipboard = (Android.Content.ClipboardManager)Activity.GetSystemService(Context.ClipboardService);
            ClipData clip = ClipData.NewPlainText("password", decryptedPassword);
            clipboard.PrimaryClip = clip;

            Toast.MakeText(Activity, "Password copied to clipboard", ToastLength.Short).Show();

            // Show security warning
            llSecurityWarning.Visibility = ViewStates.Visible;
        }

        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);

            // CRITICAL SECURITY: Clear decrypted password from memory
            if (decryptedPassword != null)
            {
                decryptedPassword = null;
                GC.Collect(); // Force garbage collection
            }
        }
    }
}