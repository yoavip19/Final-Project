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

namespace FinalProject333057891
{
    public class ForgotMasterPasswordActivity : BaseActivity
    {
        #region Properties
        public TextView tvForgotMasterPasswordField, tvForgotMasterPasswordError, tvCodeTimer;
        public EditText etForgotMasterPassword;
        public Button btnForgotMasterPassword, btnResendCode;
        public static string userNameToRecover;
        public static string userEmailToRecover;
        public static string forgotMasterPasswordCode;
        #endregion
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.forgot_master_password_layout);

            #region FindViewById
            tvForgotMasterPasswordField = FindViewById<TextView>(Resource.Id.tvForgotMasterPasswordField);
            etForgotMasterPassword = FindViewById<EditText>(Resource.Id.etForgotMasterPassword);
            tvForgotMasterPasswordError = FindViewById<TextView>(Resource.Id.tvForgotMasterPasswordError);
            btnForgotMasterPassword = FindViewById<Button>(Resource.Id.btnForgotMasterPassword);
            tvCodeTimer = FindViewById<TextView>(Resource.Id.tvCodeTimer);
            btnResendCode = FindViewById<Button>(Resource.Id.btnResendCode);
            #endregion

            UnsignedMenuFragment fragment = new UnsignedMenuFragment();
            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.flForgotMasterPasswordMenuContainer, fragment).Commit();
        }

        protected void GenerateEmailCode()
        {
            // Generates a cryptographically secure 8-character alphanumeric code
            const string CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excludes ambiguous chars (0,O,1,I)
            const int CODE_LENGTH = 8;

            byte[] randomBytes = new byte[CODE_LENGTH];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            char[] code = new char[CODE_LENGTH];
            for (int i = 0; i < CODE_LENGTH; i++)
            {
                code[i] = CHARS[randomBytes[i] % CHARS.Length];
            }

            forgotMasterPasswordCode = new string(code);
        }
    }
}