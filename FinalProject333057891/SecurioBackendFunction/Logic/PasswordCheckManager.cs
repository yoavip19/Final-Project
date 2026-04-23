using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Logic
{
    /// <summary>Computes a password-health summary for a specific user without requiring an active session.</summary>
    public class PasswordCheckManager
    {
        // Passwords (and the master password) not changed within this many days are flagged as old.
        private const int OldPasswordDays = 90;

        private readonly IUserRepository _userRepo;
        private readonly IVaultItemRepository _vaultRepo;

        /// <summary>Initializes the manager with the required repository dependencies.</summary>
        public PasswordCheckManager(IUserRepository userRepo, IVaultItemRepository vaultRepo)
        {
            _userRepo  = userRepo;
            _vaultRepo = vaultRepo;
        }

        /// <summary>Returns a PasswordCheckResult for the given user, or null when the user is not found.</summary>
        public async Task<PasswordCheckResult?> GetPasswordCheckAsync(int userId)
        {
            var user = await _userRepo.GetUserProfileAsync(userId);
            if (user == null)
                return null;

            // 1. Load all vault items for the user.
            var items = await _vaultRepo.GetVaultItemsByUserIdAsync(userId);

            int breachedCount = items.Count(i => i.IsLeaked);
            int oldCount      = items.Count(i => (DateTime.UtcNow - i.LastUpdate).TotalDays > OldPasswordDays);
            bool masterOld    = (DateTime.UtcNow - user.LastPasswordUpdate).TotalDays > OldPasswordDays;

            return new PasswordCheckResult
            {
                BreachedCount     = breachedCount,
                OldCount          = oldCount,
                MasterPasswordOld = masterOld
            };
        }
    }
}
