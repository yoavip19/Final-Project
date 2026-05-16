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
    /// <summary>Manages vault-item operations against the server.</summary>
    public class VaultService : BaseService
    {
        /// <summary>Sends an encrypted vault item to the server for storage.</summary>
        public async Task<(bool Success, string Message, VaultItem Data)> AddVaultItemAsync(VaultItem item)
        {
            var result = await PostAsync<VaultItem>("AddVaultItem", item);
            return (result.Success, result.Message, result.Data);
        }

        /// <summary>Sends an updated vault item request to the server for persistence.</summary>
        public async Task<(bool Success, string Message, VaultItem Data)> UpdateVaultItemAsync(UpdateVaultItemRequest request)
        {
            var result = await PostAsync<VaultItem>("UpdateVaultItem", request);
            return (result.Success, result.Message, result.Data);
        }

        /// <summary>Retrieves all vault items for the authenticated user.</summary>
        public async Task<(bool Success, string Message, List<VaultItem> Data)> GetVaultItemsAsync()
        {
            var result = await GetAsync<List<VaultItem>>("GetVaultItems");
            return (result.Success, result.Message, result.Data);
        }

        /// <summary>Permanently deletes a vault item from the server.</summary>
        public async Task<(bool Success, string Message)> DeleteVaultItemAsync(int itemId)
        {
            var result = await PostAsync<object>("DeleteVaultItem", new { Id = itemId });
            return (result.Success, result.Message);
        }
    }
}