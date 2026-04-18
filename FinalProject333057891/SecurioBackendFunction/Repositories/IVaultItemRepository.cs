using SecurioModels.DataTransferObjects;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Repositories
{
    // Defines the contract for all vault-item-related database operations.
    public interface IVaultItemRepository
    {
        Task<int> AddVaultItemAsync(VaultItem item);
        Task<bool> UpdateVaultItemAsync(VaultItem item);
    }
}
