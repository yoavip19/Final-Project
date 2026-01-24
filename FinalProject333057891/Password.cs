using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProject333057891
{
    [Table("Passwords")]
    public class Password
    {
        [PrimaryKey, Column("Username")]
        public string Username { get; set; }

        [PrimaryKey, Column("AppPackageID")]
        public string AppPackageID { get; set; }

        [Column("AppUsername")]
        public string AppUsername { get; set; }

        [Column("Salt")]
        public string Salt { get; set; }

        [Column("InitVector")]
        public string InitVector { get; set; }

        [Column("PasswordEncrypted")]
        public string PasswordEncrypted { get; set; }

        [Column("IsFavorite")]
        public bool IsFavorite { get; set; }


        //Future - created at

        public Password()
        {
        }

        public Password(string username, string appPackageID, string appUsername, string plainPassword, bool isFavorite)
        {
            Username = username;
            AppPackageID = appPackageID;
            AppUsername = appUsername;
            PasswordEncrypted = SecurityHelper.EncryptAES(plainPassword, "Insert master", out string salt, out string initVector); //Insert master => master password from shared preference
            Salt = salt;
            InitVector = initVector;
            IsFavorite = isFavorite;
        }

        public Password(string username, string appPackageID, string appUsername, string salt, string initVector, string passwordEncrypted, bool isFavorite)
        {
            Username = username;
            AppPackageID = appPackageID;
            AppUsername = appUsername;
            Salt = salt;
            InitVector = initVector;
            PasswordEncrypted = passwordEncrypted;
            IsFavorite = isFavorite;
        }
    }
}
