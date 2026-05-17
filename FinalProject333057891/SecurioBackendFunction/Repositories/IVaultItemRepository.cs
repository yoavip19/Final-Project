using SecurioModels.DataTransferObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Repositories
{
    /// <summary>Defines the contract for all vault-item-related database operations.</summary>
    public interface IVaultItemRepository
    {
        /// <summary>Inserts a new vault item and returns the newly generated ID.</summary>
        Task<int> AddVaultItemAsync(VaultItem item);
        /// <summary>Updates an existing vault item. When passwordChanged is true the server refreshes the LastUpdate timestamp.</summary>
        Task<bool> UpdateVaultItemAsync(VaultItem item, bool passwordChanged);
        /// <summary>Retrieves all vault items for the specified user.</summary>
        Task<List<VaultItem>> GetVaultItemsByUserIdAsync(int userId);
        /// <summary>Deletes a vault item by ID.</summary>
        Task<bool> DeleteVaultItemAsync(int itemId, int userId);
        /// <summary>Updates the IsLeaked flag for a single vault item by its ID.</summary>
        Task UpdateIsLeakedAsync(int itemId, bool isLeaked);
    }
}
