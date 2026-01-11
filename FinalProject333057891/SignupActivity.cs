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
    [Activity(Label = "SignupActivity")]
    public class SignupActivity : BaseActivity
    {
        #region Properties
        TextView tvSignupUsernameError, tvSignupEmailError, tvSignupPhoneError, tvSignupPasswordError, tvSignupConfirmPasswordError;
        EditText etSignupUsername, etSignupEmail, etSignupPhone, etSignupPassword, etSignupConfirmPassword;
        Button btnSignupSubmit, btnSignupHidePassword, btnSignupHideConfirmPassword;
        #endregion
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.signup_layout);
            // Create your application here

            #region FindViewById

            tvSignupUsernameError = FindViewById<TextView>(Resource.Id.tvSignupUsernameError);

            etSignupUsername = FindViewById<EditText>(Resource.Id.etSignupUsername);

            tvSignupEmailError = FindViewById<TextView>(Resource.Id.tvSignupEmailError);

            etSignupEmail = FindViewById<EditText>(Resource.Id.etSignupEmail);

            tvSignupPhoneError = FindViewById<TextView>(Resource.Id.tvSignupPhoneError);

            etSignupPhone = FindViewById<EditText>(Resource.Id.etSignupPhone);

            tvSignupPasswordError = FindViewById<TextView>(Resource.Id.tvSignupPasswordError);

            etSignupPassword = FindViewById<EditText>(Resource.Id.etSignupPassword);
            etSignupPassword.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;

            btnSignupHidePassword = FindViewById<Button>(Resource.Id.btnSignupHidePassword);

            tvSignupConfirmPasswordError = FindViewById<TextView>(Resource.Id.tvSignupConfirmPasswordError);

            etSignupConfirmPassword = FindViewById<EditText>(Resource.Id.etSignupConfirmPassword);
            etSignupConfirmPassword.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;

            btnSignupHideConfirmPassword = FindViewById<Button>(Resource.Id.btnSignupHideConfirmPassword);

            btnSignupSubmit = FindViewById<Button>(Resource.Id.btnSignupSubmit);

            #endregion

            UnsignedMenuFragment fragment = new UnsignedMenuFragment();
            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.flSignupMenuContainer, fragment).Commit();

            btnSignupSubmit.Click += BtnSignupSubmit_Click;
            btnSignupHidePassword.Click += BtnSignupHidePassword_Click;
            btnSignupHideConfirmPassword.Click += BtnSignupHideConfirmPassword_Click;
        }

        private void BtnSignupHidePassword_Click(object sender, EventArgs e)
        {
            HideShowPassword(etSignupPassword);
        }
        private void BtnSignupHideConfirmPassword_Click(object sender, EventArgs e)
        {
            HideShowPassword(etSignupConfirmPassword);
        }

        private void HideShowPassword(EditText etPassword)
        {
            //handles the hide/show password button
            if (etPassword.InputType == (InputTypes.ClassText | InputTypes.TextVariationPassword))
                etPassword.InputType = InputTypes.ClassText;
            else
                etPassword.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
        }

        private void BtnSignupSubmit_Click(object sender, EventArgs e)
        {
            //handles signup
            //if (IsValid())
            //{
            User user = new User(etSignupUsername.Text.Trim(), etSignupEmail.Text.Trim(), etSignupPhone.Text.Trim(), etSignupPassword.Text.Trim());
            try
            {
                dbCommand = Helper.GetDBCommand(this);
                User check = dbCommand.Find<User>(etSignupUsername.Text);
                if (check != null)
                {
                    Toast.MakeText(this, "Username already exists", ToastLength.Short).Show();
                    return;
                }

                int rowChange = dbCommand.Insert(user);
                if (rowChange > 0)
                {
                    Toast.MakeText(this, "User registered successfully", ToastLength.Short).Show();
                    StartActivity(new Intent(this, typeof(MainActivity)));
                }
                else
                {
                    Toast.MakeText(this, "Error with registering", ToastLength.Short).Show();
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Code error:\n" + ex.ToString(), ToastLength.Short).Show();
            }
            //}
        }
        private bool IsValid()
        {
            bool noError = true;
            if (!Validation.IsValidUsername(etSignupUsername.Text))
            {
                noError = false;
                tvSignupUsernameError.Visibility = ViewStates.Visible;
            }
            if (!Validation.IsValidEmail(etSignupEmail.Text))
            {
                noError = false;
                tvSignupEmailError.Visibility = ViewStates.Visible;
            }
            //if (!Validation.IsValidPhone(etSignupPhone.Text))
            //{
            //    noError = false;
            //    tvSignupPhoneError.Visibility = ViewStates.Visible;
            //}
            if (!Validation.IsStrongPassword(etSignupPassword.Text))
            {
                noError = false;
                tvSignupPasswordError.Visibility = ViewStates.Visible;
            }
            if (etSignupConfirmPassword.Text != etSignupPassword.Text)
            {
                noError = false;
                tvSignupConfirmPasswordError.Visibility = ViewStates.Visible;
            }
            return noError;
        }
    }
}