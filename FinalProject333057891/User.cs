using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject333057891
{
    [Table("Users")]
    public class User
    {

        [PrimaryKey, Column("Username")]
        public string Username { get; set; }

        [Column("Email")]
        public string Email { get; set; }

        [Column("Phone")]
        public string Phone { get; set; }

        [Column("MasterSalt")]
        public string MasterSalt { get; set; }
        
        [Column("MasterPasswordHash")]
        public string MasterPasswordHash { get; set; }

        public User()
        {
        }

        public User(string username, string email, string phone, string password)
        {
            Username = username;
            Email = email;
            Phone = phone;
            MasterSalt = SecurityHelper.GenerateSaltBase64();
            MasterPasswordHash = SecurityHelper.HashPassword(password, MasterSalt);
        }

        public User(string username, string email, string phone, string masterSalt, string masterPasswordHash)
        {
            Username = username;
            Email = email;
            Phone = phone;
            MasterSalt = masterSalt;
            MasterPasswordHash = masterPasswordHash;
        }
    }
}