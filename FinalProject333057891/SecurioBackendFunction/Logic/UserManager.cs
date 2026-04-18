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

        public UserManager(IUserRepository repo) => _repo = repo;

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
