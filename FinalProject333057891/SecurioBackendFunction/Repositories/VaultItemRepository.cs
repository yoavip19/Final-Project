using Microsoft.Data.SqlClient;
using SecurioModels.DataTransferObjects;
using System.Data;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Repositories
{
    // Manages all direct SQL database interactions for vault-item data.
    public class VaultItemRepository : IVaultItemRepository
    {
        private readonly string _connectionString;
        public VaultItemRepository(string connectionString) => _connectionString = connectionString;

        // Inserts a new vault item record and returns the newly generated ID.
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
    }
}
