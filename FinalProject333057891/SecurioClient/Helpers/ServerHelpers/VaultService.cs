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

namespace SecurioClient.Helpers.ServerHelpers
{
    // Manages vault-item operations against the server, following the same pattern as AuthService.
    public class VaultService : BaseService
    {
        // Sends an encrypted vault item to the server for storage.
        public async Task<(bool Success, string Message, VaultItem Data)> AddVaultItemAsync(VaultItem item)
        {
            var result = await PostAsync<VaultItem>("AddVaultItem", item);
            return (result.Success, result.Message, result.Data);
        }
    }
}