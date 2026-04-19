using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Logic
{
    // Coordinates the retrieval of user-specific data and account statistics.
    public class UserManager
    {
        private readonly IUserRepository _repo;
        private readonly IVaultItemRepository _vaultRepo;
        private readonly IHibpService _hibp;

        public UserManager(IUserRepository repo, IVaultItemRepository vaultRepo, IHibpService hibp)
        {
            _repo = repo;
            _vaultRepo = vaultRepo;
            _hibp = hibp;
        }

        // Fetches the profile and wraps it in a standard ProfileResponse for the API.
        public async Task<ServerResponse<User>> GetProfileAsync(int userId)
        {
            var user = await _repo.GetUserProfileAsync(userId);

            if (user == null)
            {
                return new ServerResponse<User> { Success = false, Message = "Profile not found." };
            }

            return new ServerResponse<User>
            {
                Success = true,
                Data = user
            };
        }

        // Updates the user's account details (username, email, and optionally master password).
        // When passwordChanged is true the caller must supply new MasterPasswordKey, AuthSalt and EncryptionSalt,
        // along with re-encrypted vault items.
        public async Task<ServerResponse<object>> UpdateUserAsync(int userId, User updated, bool passwordChanged, List<VaultItem> reEncryptedItems = null)
        {
            if (string.IsNullOrWhiteSpace(updated.Username))
                return new ServerResponse<object> { Success = false, Message = "Username is required." };

            if (string.IsNullOrWhiteSpace(updated.Email))
                return new ServerResponse<object> { Success = false, Message = "Email is required." };

            // Check for email uniqueness (excluding the current user)
            bool emailTaken = await _repo.EmailExistsForOtherUserAsync(updated.Email, userId);
            if (emailTaken)
                return new ServerResponse<object> { Success = false, Message = "Email is already in use by another account." };

            if (passwordChanged)
            {
                if (string.IsNullOrWhiteSpace(updated.MasterPasswordKey))
                    return new ServerResponse<object> { Success = false, Message = "MasterPasswordKey is required when changing password." };

                if (string.IsNullOrWhiteSpace(updated.AuthSalt))
                    return new ServerResponse<object> { Success = false, Message = "AuthSalt is required when changing password." };

                if (string.IsNullOrWhiteSpace(updated.EncryptionSalt))
                    return new ServerResponse<object> { Success = false, Message = "EncryptionSalt is required when changing password." };

                // HIBP breach check — same pattern as registration.
                if (!string.IsNullOrWhiteSpace(updated.PasswordSha1Hash))
                {
                    bool isPwned = await _hibp.IsPasswordPwnedAsync(updated.PasswordSha1Hash);
                    if (isPwned)
                        return new ServerResponse<object>
                        {
                            Success = false,
                            Message = "Password has been found in a data breach. Please choose a different password."
                        };
                }
            }

            updated.Id = userId;
            bool success = await _repo.UpdateUserAsync(updated, passwordChanged);

            if (!success)
                return new ServerResponse<object> { Success = false, Message = "Account not found." };

            // Bulk-update re-encrypted vault items when the master password changes
            if (passwordChanged && reEncryptedItems != null && reEncryptedItems.Count > 0)
            {
                bool vaultUpdated = await _vaultRepo.BulkUpdateVaultItemsAsync(reEncryptedItems, userId);
                if (!vaultUpdated)
                    return new ServerResponse<object> { Success = false, Message = "Account updated but vault re-encryption failed." };
            }

            return new ServerResponse<object> { Success = true, Message = "Account updated successfully." };
        }

        // Deletes the user account and all associated vault items (via CASCADE).
        public async Task<ServerResponse<object>> DeleteUserAsync(int userId)
        {
            bool deleted = await _repo.DeleteUserAsync(userId);

            if (!deleted)
            {
                return new ServerResponse<object> { Success = false, Message = "Account not found." };
            }

            return new ServerResponse<object> { Success = true, Message = "Account deleted successfully." };
        }
    }
}
