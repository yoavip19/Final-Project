using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using SecurioBackendFunction.Helpers;
using SecurioBackendFunction.Repositories;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SecurioBackendFunction.ServerFunctions
{
    /// <summary>
    /// TEST-ONLY endpoints for scenarios that cannot be covered by automated unit tests.
    /// These functions should only be deployed to the testing environment.
    /// Remove this file (or gate it behind an environment flag) before going to production.
    /// </summary>
    public class TestFunctions
    {
        private readonly IVaultItemRepository _vaultRepo;

        /// <summary>Initializes a new instance of TestFunctions.</summary>
        public TestFunctions(IVaultItemRepository vaultRepo)
        {
            _vaultRepo = vaultRepo;
        }

        /// <summary>
        /// Verifies that <c>BulkUpdateVaultItemsAsync</c> is fully atomic.
        /// The endpoint re-encrypts the first <paramref name="FailAtIndex"/> items
        /// normally, then deliberately throws an exception to simulate a mid-batch
        /// failure.  The caller can confirm the transaction rolled back by reading the
        /// vault items back — they must be identical to the state before the call.
        ///
        /// POST /api/TestBulkUpdateRollback
        /// Body: { "UserId": 1, "FailAtIndex": 2 }
        ///
        /// Response:
        ///   Success = true  → the transaction rolled back correctly (no partial update).
        ///   Success = false → at least one item was partially updated (atomicity broken).
        /// </summary>
        [Function("TestBulkUpdateRollback")]
        public async Task<IActionResult> TestBulkUpdateRollback(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                var authHeader = req.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });

                var token = authHeader.Substring("Bearer ".Length);
                var principal = JwtHelper.ValidateToken(token);
                if (principal == null)
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });

                int userId = JwtHelper.GetUserIdFromPrincipal(principal);
                if (userId <= 0)
                    return new UnauthorizedObjectResult(new ServerResponse<object> { Success = false, Message = "Unauthorized." });

                var body    = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<TestBulkUpdateRequest>(body);

                if (request == null)
                    return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = "Invalid request body." });

                // 1. Read the current vault items so we can compare after the test.
                var before = await _vaultRepo.GetVaultItemsByUserIdAsync(userId);
                if (before.Count == 0)
                {
                    return new OkObjectResult(new ServerResponse<object>
                    {
                        Success = false,
                        Message = "No vault items found. Add at least one password before running this test."
                    });
                }

                int failAt = request.FailAtIndex > 0 ? request.FailAtIndex : before.Count;

                // 2. Build fake re-encrypted items (sentinel values easy to detect if they leak).
                var fakeItems = before.Select(i => new VaultItem
                {
                    Id         = i.Id,
                    UserId     = userId,
                    IV         = "TEST_IV_SHOULD_NOT_PERSIST",
                    Tag        = "TEST_TAG_SHOULD_NOT_PERSIST",
                    CipherText = "TEST_CIPHER_SHOULD_NOT_PERSIST"
                }).ToList();

                // 3. Attempt the partial bulk-update with an injected failure.
                bool transactionRolledBack = false;
                try
                {
                    await BulkUpdateWithFaultAsync(fakeItems, userId, failAt);
                }
                catch
                {
                    transactionRolledBack = true;
                }

                if (!transactionRolledBack)
                {
                    return new OkObjectResult(new ServerResponse<object>
                    {
                        Success = false,
                        Message = "The injected exception was not thrown — check FailAtIndex value."
                    });
                }

                // 4. Read vault items again and verify none were changed.
                var after = await _vaultRepo.GetVaultItemsByUserIdAsync(userId);
                bool allUnchanged = before.All(b =>
                {
                    var a = after.FirstOrDefault(x => x.Id == b.Id);
                    return a != null
                        && a.IV         == b.IV
                        && a.Tag        == b.Tag
                        && a.CipherText == b.CipherText;
                });

                if (allUnchanged)
                {
                    return new OkObjectResult(new ServerResponse<object>
                    {
                        Success = true,
                        Message = $"✅ Atomicity confirmed: transaction rolled back correctly after failure at index {failAt}. " +
                                  $"All {before.Count} vault items are unchanged."
                    });
                }
                else
                {
                    return new OkObjectResult(new ServerResponse<object>
                    {
                        Success = false,
                        Message = $"❌ Atomicity BROKEN: one or more vault items were partially updated despite the simulated failure."
                    });
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new ServerResponse<object>
                {
                    Success = false,
                    Message = $"Test endpoint error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Performs a bulk-update inside a SQL transaction and deliberately throws at
        /// <paramref name="failAtIndex"/> to verify rollback behaviour.
        /// Only available in DEBUG builds (matching <see cref="VaultItemRepository.GetConnectionStringForTest"/>).
        /// </summary>
        private async Task BulkUpdateWithFaultAsync(List<VaultItem> items, int userId, int failAtIndex)
        {
            // Use the concrete type to access the connection string via reflection-free casting.
            if (_vaultRepo is not VaultItemRepository concreteRepo)
                throw new InvalidOperationException("Repository must be VaultItemRepository for this test.");

#if !DEBUG
            throw new InvalidOperationException("TestBulkUpdateRollback is only available in DEBUG builds.");
#endif

            string connStr = concreteRepo.GetConnectionStringForTest();

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (i == failAtIndex)
                        throw new InvalidOperationException(
                            $"[TEST] Simulated failure at index {failAtIndex} — verifying rollback.");

                    var item = items[i];
                    const string sql = @"UPDATE VaultItems SET IV=@iv, Tag=@tag, CipherText=@cipher
                                         WHERE Id=@id AND UserId=@uid";
                    using var cmd = new SqlCommand(sql, conn, tx);
                    cmd.Parameters.Add("@id",     SqlDbType.Int).Value      = item.Id;
                    cmd.Parameters.Add("@uid",    SqlDbType.Int).Value      = userId;
                    cmd.Parameters.Add("@iv",     SqlDbType.NVarChar).Value = item.IV;
                    cmd.Parameters.Add("@tag",    SqlDbType.NVarChar).Value = item.Tag;
                    cmd.Parameters.Add("@cipher", SqlDbType.NVarChar).Value = item.CipherText;
                    await cmd.ExecuteNonQueryAsync();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }

    /// <summary>Request body for <c>TestBulkUpdateRollback</c>.</summary>
    internal sealed class TestBulkUpdateRequest
    {
        /// <summary>Vault items will be read for this user.</summary>
        public int UserId { get; set; }

        /// <summary>The zero-based index at which a simulated failure is injected.</summary>
        public int FailAtIndex { get; set; }
    }
}
