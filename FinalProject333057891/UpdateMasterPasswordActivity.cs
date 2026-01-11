using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text;
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
    [Activity(Label = "UpdateMasterPasswordActivity")]
    public class UpdateMasterPasswordActivity : BaseActivity
    {
        #region Properties
        public TextView tvUpdateMasterPasswordError, tvUpdateMasterConfirmPasswordError;
        public EditText etUpdateMasterPassword, etUpdateMasterConfirmPassword;
        public Button btnUpdateMasterHidePassword, btnUpdateMasterHideConfirmPassword, btnUpdateMasterPassword;
        public string updatedUsername;
        #endregion
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.update_master_password_layout);

            #region FindViewById
            etUpdateMasterPassword = FindViewById<EditText>(Resource.Id.etUpdateMasterPassword);

            btnUpdateMasterHidePassword = FindViewById<Button>(Resource.Id.btnUpdateMasterHidePassword);

            tvUpdateMasterPasswordError = FindViewById<TextView>(Resource.Id.tvUpdateMasterPasswordError);

            etUpdateMasterConfirmPassword = FindViewById<EditText>(Resource.Id.etUpdateMasterConfirmPassword);

            btnUpdateMasterHideConfirmPassword = FindViewById<Button>(Resource.Id.btnUpdateMasterHideConfirmPassword);

            tvUpdateMasterConfirmPasswordError = FindViewById<TextView>(Resource.Id.tvUpdateMasterConfirmPasswordError);

            btnUpdateMasterPassword = FindViewById<Button>(Resource.Id.btnUpdateMasterPassword);
            #endregion

            UnsignedMenuFragment fragment = new UnsignedMenuFragment();
            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.flUpdateMasterPasswordMenuContainer, fragment).Commit();

            updatedUsername = Intent.GetStringExtra("UsernameToUpdate");

            btnUpdateMasterHidePassword.Click += BtnUpdateMasterHidePassword_Click;
            btnUpdateMasterHideConfirmPassword.Click += BtnUpdateMasterHideConfirmPassword_Click;
            btnUpdateMasterPassword.Click += BtnUpdateMasterPassword_Click;
        }

        private void BtnUpdateMasterHidePassword_Click(object sender, EventArgs e)
        {
            HideShowPassword(etUpdateMasterPassword);
        }
        private void BtnUpdateMasterHideConfirmPassword_Click(object sender, EventArgs e)
        {
            HideShowPassword(etUpdateMasterConfirmPassword);
        }
        private void HideShowPassword(EditText etPassword)
        {
            //handles the hide/show password button
            if (etPassword.InputType == (InputTypes.ClassText | InputTypes.TextVariationPassword))
                etPassword.InputType = InputTypes.ClassText;
            else
                etPassword.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
        }
        private void BtnUpdateMasterPassword_Click(object sender, EventArgs e)
        {
            //updates the password
            //if(IsValid())
            //{
            try
            {
                dbCommand = Helper.GetDBCommand(this);
                User updatedUser = dbCommand.Find<User>(updatedUsername);
                updatedUser.MasterPasswordHash = SecurityHelper.HashPassword(etUpdateMasterPassword.Text, updatedUser.MasterSalt);
                int rowChange = dbCommand.Update(updatedUser);
                if (rowChange > 0)
                {
                    Toast.MakeText(this, "Password Updated successfully", ToastLength.Long).Show();
                    StartActivity(new Intent(this, typeof(MainActivity)));
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Code error:\n" + ex.ToString(), ToastLength.Short).Show();
                etUpdateMasterConfirmPassword.Text = ex.Message;
            }
        }
        private bool IsValid()
        {
            bool noError = true;
            if (!Validation.IsStrongPassword(etUpdateMasterPassword.Text))
            {
                noError = false;
                tvUpdateMasterPasswordError.Visibility = ViewStates.Visible;
            }
            if (etUpdateMasterConfirmPassword.Text != etUpdateMasterPassword.Text)
            {
                noError = false;
                tvUpdateMasterConfirmPasswordError.Visibility = ViewStates.Visible;
            }
            return noError;
        }
    }
}