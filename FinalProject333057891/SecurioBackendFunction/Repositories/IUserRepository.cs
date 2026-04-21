using SecurioModels.DataTransferObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Repositories
{
    // Defines the contract for all user-related database operations.
    public interface IUserRepository
    {
        Task<bool> EmailExistsAsync(string email);
        Task<bool> EmailExistsForOtherUserAsync(string email, int excludeUserId);
        Task<int> CreateUserAsync(User user);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserProfileAsync(int userId);
        Task UpdateLastLoginAsync(int userId);
        Task<bool> UpdateUserAsync(User user, bool passwordChanged);
        Task<bool> DeleteUserAsync(int userId);
        Task<List<MasterPasswordHistory>> GetLastPasswordHistoryAsync(int userId, int count);
        Task AddPasswordHistoryAsync(int userId, string passwordKey, string authSalt, System.DateTime createdAt);
    }
}
