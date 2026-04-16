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
            var sql = @"INSERT INTO VaultItems (UserId, AccountName, AccountUrl, AccountUsername, EncryptedPassword, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@uid, @name, @url, @uname, @pwd, GETDATE())";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = item.UserId;
            cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = item.AccountName;
            cmd.Parameters.Add("@url", SqlDbType.NVarChar).Value = (object)item.AccountUrl ?? DBNull.Value;
            cmd.Parameters.Add("@uname", SqlDbType.NVarChar).Value = (object)item.AccountUsername ?? DBNull.Value;
            cmd.Parameters.Add("@pwd", SqlDbType.NVarChar).Value = item.EncryptedPassword;
            return (int)await cmd.ExecuteScalarAsync();
        }
    }
}
