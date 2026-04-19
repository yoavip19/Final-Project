using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Logic
{
    // Performs the periodic password-health check for a given user.
    // Counts breached passwords (via HIBP), old passwords (90+ days),
    // and checks whether the master password is stale.
    public class PasswordCheckManager
    {
        private readonly IVaultItemRepository _vaultRepo;
        private readonly IUserRepository _userRepo;
        private readonly IHibpService _hibp;

        // Passwords older than this many days are considered stale.
        private const int OldPasswordDays = 90;

        public PasswordCheckManager(
            IVaultItemRepository vaultRepo,
            IUserRepository userRepo,
            IHibpService hibp)
        {
            _vaultRepo = vaultRepo;
            _userRepo = userRepo;
            _hibp = hibp;
        }

        // Runs the full password-health check for the specified user and returns the results.
        public async Task<ServerResponse<PasswordCheckResult>> CheckAsync(int userId)
        {
            if (userId <= 0)
                return new ServerResponse<PasswordCheckResult>
                {
                    Success = false,
                    Message = "Invalid user ID."
                };

            // 1. Load all vault items for the user.
            var items = await _vaultRepo.GetVaultItemsByUserIdAsync(userId);

            // 2. Count breached passwords via HIBP k-anonymity lookup.
            int breachedCount = 0;
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Sha1Hash) &&
                    await _hibp.IsPasswordPwnedAsync(item.Sha1Hash))
                    breachedCount++;
            }

            // 3. Count old passwords (90+ days since last update).
            DateTime oldThreshold = DateTime.UtcNow.AddDays(-OldPasswordDays);
            int oldCount = 0;
            foreach (var item in items)
            {
                if (item.LastUpdate != default && item.LastUpdate < oldThreshold)
                    oldCount++;
            }

            // 4. Check whether the master password itself is old.
            bool masterOld = false;
            var user = await _userRepo.GetUserProfileAsync(userId);
            if (user != null &&
                user.LastPasswordUpdate != default &&
                user.LastPasswordUpdate < oldThreshold)
            {
                masterOld = true;
            }

            return new ServerResponse<PasswordCheckResult>
            {
                Success = true,
                Message = "Password check completed.",
                Data = new PasswordCheckResult
                {
                    BreachedCount = breachedCount,
                    OldCount = oldCount,
                    MasterPasswordOld = masterOld
                }
            };
        }
    }
}
