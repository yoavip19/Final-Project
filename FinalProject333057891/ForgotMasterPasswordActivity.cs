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
        public TextView tvForgotMasterPasswordField, tvForgotMasterPasswordError;
        public EditText etForgotMasterPassword;
        public Button btnForgotMasterPassword;
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
            #endregion

            UnsignedMenuFragment fragment = new UnsignedMenuFragment();
            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.flForgotMasterPasswordMenuContainer, fragment).Commit();
        }
    }
}