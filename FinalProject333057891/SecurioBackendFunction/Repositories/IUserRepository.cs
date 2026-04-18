using SecurioModels.DataTransferObjects;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Repositories
{
    // Defines the contract for all user-related database operations.
    public interface IUserRepository
    {
        Task<bool> EmailExistsAsync(string email);
        Task<int> CreateUserAsync(User user);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserProfileAsync(int userId);
        Task UpdateLastLoginAsync(int userId);
        Task<bool> DeleteUserAsync(int userId);
    }
}
