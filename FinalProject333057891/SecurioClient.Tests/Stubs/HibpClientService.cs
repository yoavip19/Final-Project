using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurioClient.Helpers
{
    // Test-only stub for HibpClientService.
    // No real HTTP requests are ever made; test code controls the result via
    // PwnedHashes — add a SHA-1 hash to that set to simulate a breach hit for
    // that specific hash; leave the set empty to simulate a clean check.
    //
    // Reset by calling Reset() (or clearing PwnedHashes) between tests.
    public static class HibpClientService
    {
        // The set of SHA-1 hashes that the stub will treat as "pwned".
        // Comparisons are case-insensitive.
        public static readonly HashSet<string> PwnedHashes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Returns true if sha1Hash is in PwnedHashes; false otherwise.
        // Mirrors the real service's input-validation contract (40-char hex required).
        public static Task<bool> IsPasswordPwnedAsync(string sha1Hash)
        {
            if (string.IsNullOrWhiteSpace(sha1Hash) || sha1Hash.Length != 40)
                return Task.FromResult(false);

            return Task.FromResult(PwnedHashes.Contains(sha1Hash));
        }

        // Helper: clears the pwned-hash set so each test starts with a clean slate.
        public static void Reset() => PwnedHashes.Clear();
    }
}
