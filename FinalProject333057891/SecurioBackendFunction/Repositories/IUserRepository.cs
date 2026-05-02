using SecurioModels.DataTransferObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Repositories
{
    /// <summary>Defines the contract for all user-related database operations.</summary>
    public interface IUserRepository
    {
        /// <summary>Checks whether the email is used by a different user than the excluded one.</summary>
        Task<bool> EmailExistsForOtherUserAsync(string email, int excludeUserId);
        /// <summary>
        /// Atomically inserts a new user record and returns the newly generated ID.
        /// Returns 0 if the email address is already registered, with no partial write.
        /// </summary>
        Task<int> RegisterIfEmailFreeAsync(User user);
        /// <summary>Retrieves a user record by email.</summary>
        Task<User> GetUserByEmailAsync(string email);
        /// <summary>Retrieves a full user record by user ID.</summary>
        Task<User> GetUserByIdAsync(int userId);
        /// <summary>Retrieves the profile for the given user.</summary>
        Task<User> GetUserProfileAsync(int userId);
        /// <summary>
        /// Atomically verifies credentials and stamps LastLogin in a single SQL statement.
        /// Returns the full user record if credentials match; null if not found or key is wrong.
        /// </summary>
        Task<User> VerifyLoginAndUpdateLastLoginAsync(string email, string key);
        /// <summary>Updates the user's profile fields.</summary>
        Task<bool> UpdateUserAsync(User user, bool passwordChanged);
        /// <summary>Deletes the user account.</summary>
        Task<bool> DeleteUserAsync(int userId);
        /// <summary>Returns the most-recent password history entries for the given user.</summary>
        Task<List<MasterPasswordHistory>> GetLastPasswordHistoryAsync(int userId, int count);
        /// <summary>
        /// Atomically updates user credentials, archives the old password key, and re-encrypts all
        /// vault items in a single SQL transaction so that a partial server failure can never leave
        /// the new master-password key in place while vault items are still encrypted with the old key.
        /// </summary>
        Task<bool> UpdateUserAndVaultAsync(User user, string oldPasswordKey, string oldAuthSalt,
            System.DateTime archivedAt, List<VaultItem> reEncryptedItems, int userId);
    }
}
