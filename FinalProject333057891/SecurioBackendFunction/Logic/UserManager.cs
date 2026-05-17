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
    /// <summary>Coordinates the retrieval of user-specific data and account statistics.</summary>
    public class UserManager
    {
        private readonly IUserRepository _repo;

        /// <summary>Initializes a new instance of UserManager.</summary>
        public UserManager(IUserRepository repo)
        {
            _repo = repo;
        }

        /// <summary>Fetches the profile and wraps it in a standard response for the API.</summary>
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

        /// <summary>Updates the user's account details including username, email, and optionally master password.</summary>
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

                // Fetch the current user record so we can archive the old password key before overwriting it.
                oldUser = await _repo.GetUserByIdAsync(userId);
                if (oldUser == null)
                    return new ServerResponse<object> { Success = false, Message = "Account not found." };
            }

            updated.Id = userId;

            if (passwordChanged)
            {
                // Execute credentials update, password-history archival, and vault re-encryption
                // atomically in a single SQL transaction.  Any partial failure rolls back the whole
                // unit so the database is never left with new credentials but old vault ciphertext.
                DateTime archivedAt = oldUser.LastPasswordUpdate != DateTime.MinValue
                    ? oldUser.LastPasswordUpdate
                    : (oldUser.CreatedAt != DateTime.MinValue ? oldUser.CreatedAt : DateTime.UtcNow);

                var items = reEncryptedItems ?? new List<VaultItem>();
                bool success = await _repo.UpdateUserAndVaultAsync(
                    updated, oldUser.MasterPasswordKey, oldUser.AuthSalt, archivedAt, items, userId);

                if (!success)
                    return new ServerResponse<object> { Success = false, Message = "Account not found." };
            }
            else
            {
                bool success = await _repo.UpdateUserAsync(updated);
                if (!success)
                    return new ServerResponse<object> { Success = false, Message = "Account not found." };
            }

            return new ServerResponse<object> { Success = true, Message = "Account updated successfully." };
        }

        /// <summary>Deletes the user account and all associated vault items via CASCADE.</summary>
        public async Task<ServerResponse<object>> DeleteUserAsync(int userId)
        {
            bool deleted = await _repo.DeleteUserAsync(userId);

            if (!deleted)
            {
                return new ServerResponse<object> { Success = false, Message = "Account not found." };
            }

            return new ServerResponse<object> { Success = true, Message = "Account deleted successfully." };
        }

        /// <summary>Returns the last 4 password-history entries for the user.</summary>
        public async Task<ServerResponse<List<MasterPasswordHistory>>> GetPasswordHistoryAsync(int userId)
        {
            var history = await _repo.GetLastPasswordHistoryAsync(userId, 4);
            return new ServerResponse<List<MasterPasswordHistory>> { Success = true, Data = history };
        }
    }
}
