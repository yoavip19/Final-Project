using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Logic
{
    // Coordinates the storage of encrypted vault items for a given user.
    public class VaultItemManager
    {
        private readonly IVaultItemRepository _repo;

        public VaultItemManager(IVaultItemRepository repo) => _repo = repo;

        // Validates the incoming vault item and persists it to the database.
        public async Task<ServerResponse<VaultItem>> AddVaultItemAsync(VaultItem item)
        {
            if (string.IsNullOrWhiteSpace(item.AccountName))
                return new ServerResponse<VaultItem> { Success = false, Message = "Account name is required." };

            if (string.IsNullOrWhiteSpace(item.CipherText))
                return new ServerResponse<VaultItem> { Success = false, Message = "CipherText is required." };

            if (string.IsNullOrWhiteSpace(item.IV))
                return new ServerResponse<VaultItem> { Success = false, Message = "IV is required." };

            if (string.IsNullOrWhiteSpace(item.Tag))
                return new ServerResponse<VaultItem> { Success = false, Message = "Tag is required." };

            if (string.IsNullOrWhiteSpace(item.Sha1Hash))
                return new ServerResponse<VaultItem> { Success = false, Message = "Sha1Hash is required." };

            int newId = await _repo.AddVaultItemAsync(item);
            if (newId <= 0)
                return new ServerResponse<VaultItem> { Success = false, Message = "Database error." };

            item.Id = newId;
            return new ServerResponse<VaultItem>
            {
                Success = true,
                Message = "Vault item added successfully.",
                Data = item
            };
        }
    }
}
