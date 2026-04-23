using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Logic
{
    /// <summary>Coordinates the storage of encrypted vault items for a given user.</summary>
    public class VaultItemManager
    {
        private readonly IVaultItemRepository _repo;
        private readonly IHibpService _hibp;

        /// <summary>Initializes a new instance of VaultItemManager.</summary>
        public VaultItemManager(IVaultItemRepository repo, IHibpService hibp = null)
        {
            _repo = repo;
            _hibp = hibp;
        }

        /// <summary>Validates the incoming vault item and persists it to the database.</summary>
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

            // Check HIBP and set the IsLeaked flag before persisting.
            if (_hibp != null)
                item.IsLeaked = await _hibp.IsPasswordPwnedAsync(item.Sha1Hash);

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

        /// <summary>Validates the updated vault item and persists the changes to the database.</summary>
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

            // When the password was changed, re-check HIBP to update the leaked status.
            if (item.PasswordChanged && _hibp != null)
                item.IsLeaked = await _hibp.IsPasswordPwnedAsync(item.Sha1Hash);

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

        /// <summary>Retrieves all vault items for the specified user.</summary>
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

        /// <summary>Validates the request and permanently deletes the specified vault item from the database.</summary>
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
