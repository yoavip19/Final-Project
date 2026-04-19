using SecurioModels.DataTransferObjects;
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
        Task<User> GetUserProfileAsync(int userId);
        Task UpdateLastLoginAsync(int userId);
        Task<bool> UpdateUserAsync(User user, bool passwordChanged);
        Task<bool> DeleteUserAsync(int userId);
    }
}
