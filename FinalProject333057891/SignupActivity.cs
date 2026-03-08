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
        Spinner spinnerPhonePrefix;
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

            spinnerPhonePrefix = FindViewById<Spinner>(Resource.Id.spinnerPhonePrefix);

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

            // Populate prefix options
            var prefixes = new List<string> { "050", "051", "052", "053", "054", "055", "056", "057", "058", "059", "02", "03", "04" };
            var prefixAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, prefixes);
            prefixAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinnerPhonePrefix.Adapter = prefixAdapter;

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
            string fullPhone = spinnerPhonePrefix.SelectedItem.ToString() + etSignupPhone.Text.Trim();
            User user = new User(etSignupUsername.Text.Trim(), etSignupEmail.Text.Trim(), fullPhone, etSignupPassword.Text.Trim());
            try
            {

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
            if (!Validation.IsValidUsername(etSignupUsername.Text.Trim()))
            {
                noError = false;
                tvSignupUsernameError.Visibility = ViewStates.Visible;
            }
            if (!Validation.IsUniqueUsername(this, etSignupUsername.Text.Trim()))
            {
                noError = false;
                tvSignupUsernameError.Visibility = ViewStates.Visible;
            }
            if (!Validation.IsValidEmail(etSignupEmail.Text.Trim()))
            {
                noError = false;
                tvSignupEmailError.Visibility = ViewStates.Visible;
            }
            if (!Validation.IsUniqueEmail(this, etSignupEmail.Text.Trim()))
            {
                noError = false;
                tvSignupEmailError.Visibility = ViewStates.Visible;
            }
            if (!Validation.IsValidPhone(etSignupPhone.Text.Trim()))
            {
                noError = false;
                tvSignupPhoneError.Visibility = ViewStates.Visible;
            }
            if (!Validation.IsUniquePhone(this, spinnerPhonePrefix.SelectedItem.ToString() + etSignupPhone.Text.Trim()))
            {
                noError = false;
                tvSignupPhoneError.Visibility = ViewStates.Visible;
            }
            if (!Validation.IsStrongPassword(etSignupPassword.Text.Trim()))
            {
                noError = false;
                tvSignupPasswordError.Visibility = ViewStates.Visible;
            }
            if (etSignupConfirmPassword.Text.Trim() != etSignupPassword.Text.Trim())
            {
                noError = false;
                tvSignupConfirmPasswordError.Visibility = ViewStates.Visible;
            }
            return noError;
        }
    }
}