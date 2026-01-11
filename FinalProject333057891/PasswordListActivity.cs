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

namespace FinalProject333057891
{
    [Activity(Label = "PasswordListActivity")]
    public class PasswordListActivity : BaseActivity
    {
        #region Properties
        Button btnAddPasswordToList;
        #endregion
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.password_list_layout);
            // Create your application here

            #region FindViewById
            btnAddPasswordToList = FindViewById<Button>(Resource.Id.btnAddPasswordToList);
            #endregion

            btnAddPasswordToList.Click += BtnAddPasswordToList_Click;
        }

        private void BtnAddPasswordToList_Click(object sender, EventArgs e)
        {
            Toast.MakeText(this, "Add", ToastLength.Short).Show();
        }
    }
}