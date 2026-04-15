using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SecurioModels.DataTransferObjects;
using SecurioModels;
using SecurioModels.DataTransferObjects;

namespace SecurioClient.Helpers.ServerHelpers
{
    // Manages the retrieval and caching of user profile information.
    public class ProfileService : BaseService
    {
        // Fetches the full profile stats from the server.
        public async Task<ServerResponse<User>> GetProfileAsync()
        {
            var response = await GetAsync<User>("GetProfile");

            if (response.Success && response.Data != null)
            {
                // Cache it for the next time they open the page
                await StorageHelper.SaveProfileAsync(response.Data);
            }

            return response;
        }
    }
}
