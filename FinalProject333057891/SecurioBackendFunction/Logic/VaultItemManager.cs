using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
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
            item.LastUpdate = DateTime.UtcNow;
            return new ServerResponse<VaultItem>
            {
                Success = true,
                Message = "Vault item added successfully.",
                Data = item
            };
        }

        // Validates the updated vault item and persists the changes to the database.
        public async Task<ServerResponse<VaultItem>> UpdateVaultItemAsync(VaultItem item)
        {
            if (item.Id <= 0)
                return new ServerResponse<VaultItem> { Success = false, Message = "Item ID is required." };

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

            bool updated = await _repo.UpdateVaultItemAsync(item);
            if (!updated)
                return new ServerResponse<VaultItem> { Success = false, Message = "Item not found or access denied." };

            if (item.PasswordChanged)
                item.LastUpdate = DateTime.UtcNow;
            return new ServerResponse<VaultItem>
            {
                Success = true,
                Message = "Vault item updated successfully.",
                Data = item
            };
        }

        // Retrieves all vault items for the specified user.
        public async Task<ServerResponse<List<VaultItem>>> GetVaultItemsAsync(int userId)
        {
            if (userId <= 0)
                return new ServerResponse<List<VaultItem>> { Success = false, Message = "Invalid user ID." };

            var items = await _repo.GetVaultItemsByUserIdAsync(userId);
            return new ServerResponse<List<VaultItem>>
            {
                Success = true,
                Message = "Vault items retrieved successfully.",
                Data = items
            };
        }

        // Validates the request and permanently deletes the specified vault item from the database.
        public async Task<ServerResponse<object>> DeleteVaultItemAsync(int itemId, int userId)
        {
            if (itemId <= 0)
                return new ServerResponse<object> { Success = false, Message = "Item ID is required." };

            if (userId <= 0)
                return new ServerResponse<object> { Success = false, Message = "Invalid user ID." };

            bool deleted = await _repo.DeleteVaultItemAsync(itemId, userId);
            if (!deleted)
                return new ServerResponse<object> { Success = false, Message = "Item not found or access denied." };

            return new ServerResponse<object> { Success = true, Message = "Vault item deleted successfully." };
        }
    }
}
