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

            if (string.IsNullOrWhiteSpace(item.EncryptedPassword))
                return new ServerResponse<VaultItem> { Success = false, Message = "Encrypted password is required." };

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
