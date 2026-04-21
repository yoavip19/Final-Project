using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Repositories;
using SecurioModels;
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

            User oldUser = null;
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

                // Fetch the current user record so we can archive the old password key before overwriting it.
                oldUser = await _repo.GetUserByIdAsync(userId);
                if (oldUser == null)
                    return new ServerResponse<object> { Success = false, Message = "Account not found." };
            }

            updated.Id = userId;
            bool success = await _repo.UpdateUserAsync(updated, passwordChanged);

            if (!success)
                return new ServerResponse<object> { Success = false, Message = "Account not found." };

            // When the password changed, save the old key to MasterPasswordHistory so the
            // no-reuse check can compare against it later.
            if (passwordChanged && oldUser != null)
            {
                DateTime archivedAt = oldUser.LastPasswordUpdate != DateTime.MinValue
                    ? oldUser.LastPasswordUpdate
                    : DateTime.UtcNow;
                await _repo.AddPasswordHistoryAsync(userId, oldUser.MasterPasswordKey, oldUser.AuthSalt, archivedAt);
            }

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

        // Returns the last 4 password-history entries for the user, used by the client for the no-reuse check.
        public async Task<ServerResponse<List<MasterPasswordHistory>>> GetPasswordHistoryAsync(int userId)
        {
            var history = await _repo.GetLastPasswordHistoryAsync(userId, 4);
            return new ServerResponse<List<MasterPasswordHistory>> { Success = true, Data = history };
        }
    }
}
