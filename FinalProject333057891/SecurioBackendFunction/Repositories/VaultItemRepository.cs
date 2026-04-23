using Microsoft.Data.SqlClient;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Repositories
{
    /// <summary>Manages all direct SQL database interactions for vault-item data.</summary>
    public class VaultItemRepository : IVaultItemRepository
    {
        private readonly string _connectionString;
        /// <summary>Initializes a new instance of VaultItemRepository.</summary>
        public VaultItemRepository(string connectionString) => _connectionString = connectionString;

        /// <summary>Inserts a new vault item record and returns the newly generated ID.</summary>
        public async Task<int> AddVaultItemAsync(VaultItem item)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = @"INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
                    OUTPUT INSERTED.Id
                    VALUES (@uid, @name, @uname, @iv, @tag, @cipher, @notes, @hash, @leaked, GETDATE())";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@uid",    SqlDbType.Int).Value      = item.UserId;
            cmd.Parameters.Add("@name",   SqlDbType.NVarChar).Value = item.AccountName;
            cmd.Parameters.Add("@uname",  SqlDbType.NVarChar).Value = (object)item.AccountUsername ?? DBNull.Value;
            cmd.Parameters.Add("@iv",     SqlDbType.NVarChar).Value = item.IV;
            cmd.Parameters.Add("@tag",    SqlDbType.NVarChar).Value = item.Tag;
            cmd.Parameters.Add("@cipher", SqlDbType.NVarChar).Value = item.CipherText;
            cmd.Parameters.Add("@notes",  SqlDbType.NVarChar).Value = (object)item.Notes ?? DBNull.Value;
            cmd.Parameters.Add("@hash",   SqlDbType.NVarChar).Value = item.Sha1Hash;
            cmd.Parameters.Add("@leaked", SqlDbType.Bit).Value      = item.IsLeaked;
            return (int)await cmd.ExecuteScalarAsync();
        }

        /// <summary>Updates an existing vault item, enforcing ownership via UserId.</summary>
        public async Task<bool> UpdateVaultItemAsync(VaultItem item)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = @"UPDATE VaultItems
                        SET AccountName     = @name,
                            AccountUsername  = @uname,
                            IV              = @iv,
                            Tag             = @tag,
                            CipherText      = @cipher,
                            Notes           = @notes,
                            Sha1Hash        = @hash,
                            IsLeaked        = @leaked,
                            LastUpdate      = CASE WHEN @passwordChanged = 1 THEN GETDATE() ELSE LastUpdate END
                        WHERE Id = @id AND UserId = @uid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@id",     SqlDbType.Int).Value      = item.Id;
            cmd.Parameters.Add("@uid",    SqlDbType.Int).Value      = item.UserId;
            cmd.Parameters.Add("@name",   SqlDbType.NVarChar).Value = item.AccountName;
            cmd.Parameters.Add("@uname",  SqlDbType.NVarChar).Value = (object)item.AccountUsername ?? DBNull.Value;
            cmd.Parameters.Add("@iv",     SqlDbType.NVarChar).Value = item.IV;
            cmd.Parameters.Add("@tag",    SqlDbType.NVarChar).Value = item.Tag;
            cmd.Parameters.Add("@cipher", SqlDbType.NVarChar).Value = item.CipherText;
            cmd.Parameters.Add("@notes",  SqlDbType.NVarChar).Value = (object)item.Notes ?? DBNull.Value;
            cmd.Parameters.Add("@hash",   SqlDbType.NVarChar).Value = item.Sha1Hash;
            cmd.Parameters.Add("@leaked", SqlDbType.Bit).Value      = item.IsLeaked;
            cmd.Parameters.Add("@passwordChanged", SqlDbType.Bit).Value = item.PasswordChanged;
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>Retrieves all vault items belonging to a specific user.</summary>
        public async Task<List<VaultItem>> GetVaultItemsByUserIdAsync(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = @"SELECT Id, UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate
                        FROM VaultItems
                        WHERE UserId = @uid
                        ORDER BY LastUpdate DESC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = userId;

            var items = new List<VaultItem>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new VaultItem
                {
                    Id              = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId          = reader.GetInt32(reader.GetOrdinal("UserId")),
                    AccountName     = reader.IsDBNull(reader.GetOrdinal("AccountName")) ? null : reader.GetString(reader.GetOrdinal("AccountName")),
                    AccountUsername = reader.IsDBNull(reader.GetOrdinal("AccountUsername")) ? null : reader.GetString(reader.GetOrdinal("AccountUsername")),
                    IV              = reader.IsDBNull(reader.GetOrdinal("IV")) ? null : reader.GetString(reader.GetOrdinal("IV")),
                    Tag             = reader.IsDBNull(reader.GetOrdinal("Tag")) ? null : reader.GetString(reader.GetOrdinal("Tag")),
                    CipherText      = reader.IsDBNull(reader.GetOrdinal("CipherText")) ? null : reader.GetString(reader.GetOrdinal("CipherText")),
                    Notes           = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                    Sha1Hash        = reader.IsDBNull(reader.GetOrdinal("Sha1Hash")) ? null : reader.GetString(reader.GetOrdinal("Sha1Hash")),
                    IsLeaked        = reader.GetBoolean(reader.GetOrdinal("IsLeaked")),
                    LastUpdate      = reader.GetDateTime(reader.GetOrdinal("LastUpdate"))
                });
            }
            return items;
        }

        /// <summary>Deletes a vault item by ID, enforcing ownership via UserId.</summary>
        public async Task<bool> DeleteVaultItemAsync(int itemId, int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = "DELETE FROM VaultItems WHERE Id = @id AND UserId = @uid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@id",  SqlDbType.Int).Value = itemId;
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = userId;
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>Bulk-updates the encryption fields for all vault items belonging to the given user.</summary>
        public async Task<bool> BulkUpdateVaultItemsAsync(List<VaultItem> items, int userId)
        {
            if (items == null || items.Count == 0) return true;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                foreach (var item in items)
                {
                    var sql = @"UPDATE VaultItems
                                SET IV = @iv, Tag = @tag, CipherText = @cipher
                                WHERE Id = @id AND UserId = @uid";
                    using var cmd = new SqlCommand(sql, conn, transaction);
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = item.Id;
                    cmd.Parameters.Add("@uid", SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@iv", SqlDbType.NVarChar).Value = item.IV;
                    cmd.Parameters.Add("@tag", SqlDbType.NVarChar).Value = item.Tag;
                    cmd.Parameters.Add("@cipher", SqlDbType.NVarChar).Value = item.CipherText;
                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }
    }
}
