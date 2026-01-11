using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject333057891
{
    [Activity(Label = "LoginActivity")]
    public class LoginActivity : BaseActivity
    {
        #region Properties
        TextView tvLoginUsernameError, tvLoginPasswordError, tvLoginForgotPassword;
        EditText etLoginUsername, etLoginPassword;
        Button btnLoginSubmit, btnLoginHidePassword;
        #endregion
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.login_layout);
            // Create your application here

            #region FindViewById

            tvLoginUsernameError = FindViewById<TextView>(Resource.Id.tvLoginUsernameError);

            etLoginUsername = FindViewById<EditText>(Resource.Id.etLoginUsername);

            tvLoginPasswordError = FindViewById<TextView>(Resource.Id.tvLoginPasswordError);

            etLoginPassword = FindViewById<EditText>(Resource.Id.etLoginPassword);
            etLoginPassword.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;

            btnLoginHidePassword = FindViewById<Button>(Resource.Id.btnLoginHidePassword);

            tvLoginForgotPassword = FindViewById<TextView>(Resource.Id.tvLoginForgotPassword);

            btnLoginSubmit = FindViewById<Button>(Resource.Id.btnLoginSubmit);

            #endregion

            UnsignedMenuFragment fragment = new UnsignedMenuFragment();
            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.flLoginMenuContainer, fragment).Commit();

            btnLoginSubmit.Click += BtnLoginSubmit_Click;
            btnLoginHidePassword.Click += HideShowPassword;
            tvLoginForgotPassword.Click += TvLoginForgotPassword_Click;
        }

        private void TvLoginForgotPassword_Click(object sender, EventArgs e)
        {
            StartActivity(new Intent(this, typeof(ForgotMasterPasswordEmailActivity)));
        }

        private void HideShowPassword(object sender, EventArgs e)
        {
            //handles the hide/show password button
            if (etLoginPassword.InputType == (InputTypes.ClassText | InputTypes.TextVariationPassword))
                etLoginPassword.InputType = InputTypes.ClassText;
            else
                etLoginPassword.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
        }

        private void BtnLoginSubmit_Click(object sender, EventArgs e)
        {
            //handles login
            try
            {
                dbCommand = Helper.GetDBCommand(this);
                string query = $"SELECT * FROM Users WHERE Username = ?;";
                var userList = dbCommand.Query<User>(query, etLoginUsername.Text.Trim());
                if (userList.Count > 0)
                {
                    User loginUser = userList[0];
                    string salt = loginUser.MasterSalt;
                    string hashedPassword = SecurityHelper.HashPassword(etLoginPassword.Text.Trim(), salt);

                    if (hashedPassword == loginUser.MasterPasswordHash)
                    {
                        Toast.MakeText(this, "User logged in successfully", ToastLength.Short).Show();

                        UpdateSharedPreference(loginUser);

                        StartActivity(new Intent(this, typeof(HomepageActivity)));
                    }
                    else
                    {
                        Toast.MakeText(this, "Incorrect password", ToastLength.Short).Show();
                    }
                }
                else
                {
                    Toast.MakeText(this, "User does not exist", ToastLength.Short).Show();
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Code error:\n" + ex.ToString(), ToastLength.Short).Show();
            }
        }

        private void UpdateSharedPreference(User user)
        {
            var editor = sp.Edit();
            editor.PutString("Username", user.Username);
            editor.PutString("Email", user.Email);
            editor.PutString("Phone", user.Phone);
            editor.PutString("MasterPassword", etLoginPassword.Text.Trim());
            editor.Commit();
        }
    }
}