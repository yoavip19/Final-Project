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
        /// <summary>Updates an existing vault item.</summary>
        Task<bool> UpdateVaultItemAsync(VaultItem item);
        /// <summary>Retrieves all vault items for the specified user.</summary>
        Task<List<VaultItem>> GetVaultItemsByUserIdAsync(int userId);
        /// <summary>Deletes a vault item by ID.</summary>
        Task<bool> DeleteVaultItemAsync(int itemId, int userId);
        /// <summary>Bulk-updates the encryption fields for all vault items belonging to the given user.</summary>
        Task<bool> BulkUpdateVaultItemsAsync(List<VaultItem> items, int userId);
    }
}
