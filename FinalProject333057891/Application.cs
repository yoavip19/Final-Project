using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using System.Net.Http;
using Android.Graphics;
using System.IO;

namespace FinalProject333057891
{
    [Table("Applications")]
    public class Application
    {
        [PrimaryKey, Column("PackageID")]
        public string PackageID { get; set; }

        [Column("AppName")]
        public string AppName { get; set; }

        [Column("IconBase64")]
        public string IconBase64 { get; set; }

        [Column("Category")]
        public string Category { get; set; }

        public Application()
        {
        }
        public Application(string packageID, string appName, string iconBase64, string category)
        {
            PackageID = packageID;
            AppName = appName;
            IconBase64 = iconBase64;
            Category = category;
        }

        public static async Task<string> DownloadImageAsBase64Async(string imageUrl)
        {
            if (imageUrl == "") { throw new Exception(); }
            using (var httpClient = new HttpClient())
            {
                byte[] imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                return Convert.ToBase64String(imageBytes);
            }
        }
        public static Bitmap Base64ToBitmap(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
        }
        public static string BitmapToBase64(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
                return Convert.ToBase64String(stream.ToArray());
            }
        }
    }
}
