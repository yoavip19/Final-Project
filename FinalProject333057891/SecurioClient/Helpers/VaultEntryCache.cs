using SecurioModels.DataTransferObjects;
using System.Collections.Generic;

namespace SecurioClient.Helpers
{
    /// <summary>Static cache used to share the current entry list between VaultActivity and entry activities for duplicate checking.</summary>
    public static class VaultEntryCache
    {
        private static List<VaultItem> _entries = new List<VaultItem>();

        /// <summary>Gets the cached vault entries. Use <see cref="SetEntries"/> to update the list.</summary>
        public static List<VaultItem> Entries => _entries;

        /// <summary>Replaces the cached entry list with the provided collection.</summary>
        public static void SetEntries(List<VaultItem> entries)
        {
            _entries = entries ?? new List<VaultItem>();
        }
    }
}
