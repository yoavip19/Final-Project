using SecurioModels;
using SecurioModels.DataTransferObjects;
using System.Threading.Tasks;

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
