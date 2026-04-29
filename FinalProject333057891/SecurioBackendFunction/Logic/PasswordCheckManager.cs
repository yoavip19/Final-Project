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
        private readonly IHibpService _hibp;

        /// <summary>Initializes the manager with repository and HIBP dependencies.</summary>
        public PasswordCheckManager(IUserRepository userRepo, IVaultItemRepository vaultRepo, IHibpService hibp)
        {
            _userRepo  = userRepo;
            _vaultRepo = vaultRepo;
            _hibp      = hibp;
        }

        /// <summary>Returns a PasswordCheckResult for the given user, or null when the user is not found.</summary>
        public async Task<PasswordCheckResult?> GetPasswordCheckAsync(int userId)
        {
            var user = await _userRepo.GetUserProfileAsync(userId);
            if (user == null)
                return null;

            // 1. Load all vault items for the user.
            var items = await _vaultRepo.GetVaultItemsByUserIdAsync(userId);

            // 2. Re-check HIBP for every item that has a stored SHA-1 hash and update the DB
            //    when the breach status has changed.  This is the sole legitimate server-side
            //    HIBP use: the background service can detect new breaches for passwords that
            //    were clean when originally saved, without the user having to re-edit them.
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Sha1Hash)) continue;

                bool nowLeaked = await _hibp.IsPasswordPwnedAsync(item.Sha1Hash);
                if (nowLeaked != item.IsLeaked)
                {
                    item.IsLeaked = nowLeaked;
                    await _vaultRepo.UpdateIsLeakedAsync(item.Id, nowLeaked);
                }
            }

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
