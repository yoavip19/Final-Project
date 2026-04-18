using SecurioModels.DataTransferObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Repositories
{
    // Defines the contract for all vault-item-related database operations.
    public interface IVaultItemRepository
    {
        Task<int> AddVaultItemAsync(VaultItem item);
        Task<bool> UpdateVaultItemAsync(VaultItem item);
        Task<List<VaultItem>> GetVaultItemsByUserIdAsync(int userId);
    }
}
