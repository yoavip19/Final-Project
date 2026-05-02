using Microsoft.Data.SqlClient;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecurioBackendFunction.Repositories
{
    /// <summary>Manages all direct SQL database interactions for user-related data.</summary>
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;
        /// <summary>Initializes a new instance of UserRepository.</summary>
        public UserRepository(string connectionString) => _connectionString = connectionString;

        /// <summary>Checks the database to see if a specific email address is already in use.</summary>
        public async Task<bool> EmailExistsAsync(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = "SELECT COUNT(1) FROM Users WHERE Email = @email";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@email", SqlDbType.NVarChar).Value = email;
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        /// <summary>Inserts a new user record and returns the newly generated ID.</summary>
        public async Task<int> CreateUserAsync(User user)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = @"INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt) 
                    OUTPUT INSERTED.Id
                    VALUES (@name, @email, @key, @asalt, @esalt, GETDATE(), GETDATE(), GETDATE())";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = user.Username;
            cmd.Parameters.Add("@email", SqlDbType.NVarChar).Value = user.Email;
            cmd.Parameters.Add("@key", SqlDbType.NVarChar).Value = user.MasterPasswordKey;
            cmd.Parameters.Add("@asalt", SqlDbType.NVarChar).Value = user.AuthSalt;
            cmd.Parameters.Add("@esalt", SqlDbType.NVarChar).Value = user.EncryptionSalt;
            return (int)await cmd.ExecuteScalarAsync();
        }

        /// <summary>Retrieves a user record by email for authentication purposes.</summary>
        public async Task<User> GetUserByEmailAsync(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = @"SELECT Id, Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, 
                       LastLogin, LastPasswordUpdate, CreatedAt 
                FROM Users WHERE Email = @email";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@email", SqlDbType.NVarChar).Value = email;
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = (int)reader["Id"],
                    Username = reader["Username"].ToString(),
                    Email = reader["Email"].ToString(),
                    MasterPasswordKey = reader["MasterPasswordKey"].ToString(),
                    AuthSalt = reader["AuthSalt"].ToString(),
                    EncryptionSalt = reader["EncryptionSalt"].ToString(),
                    LastLogin = reader["LastLogin"] != DBNull.Value ? (DateTime)reader["LastLogin"] : DateTime.MinValue,
                    LastPasswordUpdate = reader["LastPasswordUpdate"] != DBNull.Value ? (DateTime)reader["LastPasswordUpdate"] : DateTime.MinValue,
                    CreatedAt = reader["CreatedAt"] != DBNull.Value ? (DateTime)reader["CreatedAt"] : DateTime.MinValue
                };
            }
            return null;
        }


        /// <summary>Retrieves the fully populated profile for the given user.</summary>
        public async Task<User> GetUserProfileAsync(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"SELECT Id, Username, Email, CreatedAt, LastLogin, LastPasswordUpdate,
                (SELECT COUNT(*) FROM VaultItems WHERE UserId = @uid) AS PasswordCount
                FROM Users WHERE Id = @uid";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = userId;

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = (int)reader["Id"],
                    Username = reader["Username"].ToString(),
                    Email = reader["Email"].ToString(),
                    CreatedAt = reader["CreatedAt"] != DBNull.Value ? (DateTime)reader["CreatedAt"] : DateTime.MinValue,
                    LastLogin = reader["LastLogin"] != DBNull.Value ? (DateTime)reader["LastLogin"] : DateTime.MinValue,
                    LastPasswordUpdate = reader["LastPasswordUpdate"] != DBNull.Value ? (DateTime)reader["LastPasswordUpdate"] : DateTime.MinValue,
                    PasswordCount = (int)reader["PasswordCount"]
                };
            }
            return null;
        }
        /// <summary>Updates the LastLogin timestamp for the given user to the current UTC time.</summary>
        public async Task UpdateLastLoginAsync(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = "UPDATE Users SET LastLogin = GETDATE() WHERE Id = @uid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = userId;
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Checks whether the email is already used by a different user.</summary>
        public async Task<bool> EmailExistsForOtherUserAsync(string email, int excludeUserId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = "SELECT COUNT(1) FROM Users WHERE Email = @email AND Id != @uid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@email", SqlDbType.NVarChar).Value = email;
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = excludeUserId;
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        /// <summary>Updates the user's profile fields, including password fields when passwordChanged is true.</summary>
        public async Task<bool> UpdateUserAsync(User user, bool passwordChanged)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql;
            if (passwordChanged)
            {
                sql = @"UPDATE Users 
                        SET Username = @name, Email = @email,
                            MasterPasswordKey = @key, AuthSalt = @asalt, EncryptionSalt = @esalt,
                            LastPasswordUpdate = GETDATE()
                        WHERE Id = @uid";
            }
            else
            {
                sql = @"UPDATE Users 
                        SET Username = @name, Email = @email
                        WHERE Id = @uid";
            }

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = user.Id;
            cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = user.Username;
            cmd.Parameters.Add("@email", SqlDbType.NVarChar).Value = user.Email;

            if (passwordChanged)
            {
                cmd.Parameters.Add("@key", SqlDbType.NVarChar).Value = user.MasterPasswordKey;
                cmd.Parameters.Add("@asalt", SqlDbType.NVarChar).Value = user.AuthSalt;
                cmd.Parameters.Add("@esalt", SqlDbType.NVarChar).Value = user.EncryptionSalt;
            }

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>Deletes the user account; CASCADE removes all associated vault items automatically.</summary>
        public async Task<bool> DeleteUserAsync(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = "DELETE FROM Users WHERE Id = @uid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = userId;
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>Retrieves a full user record including password key and salts by user ID.</summary>
        public async Task<User> GetUserByIdAsync(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = @"SELECT Id, Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt,
                               LastLogin, LastPasswordUpdate, CreatedAt
                        FROM Users WHERE Id = @uid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = userId;
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = (int)reader["Id"],
                    Username = reader["Username"].ToString(),
                    Email = reader["Email"].ToString(),
                    MasterPasswordKey = reader["MasterPasswordKey"].ToString(),
                    AuthSalt = reader["AuthSalt"].ToString(),
                    EncryptionSalt = reader["EncryptionSalt"].ToString(),
                    LastLogin = reader["LastLogin"] != DBNull.Value ? (DateTime)reader["LastLogin"] : DateTime.MinValue,
                    LastPasswordUpdate = reader["LastPasswordUpdate"] != DBNull.Value ? (DateTime)reader["LastPasswordUpdate"] : DateTime.MinValue,
                    CreatedAt = reader["CreatedAt"] != DBNull.Value ? (DateTime)reader["CreatedAt"] : DateTime.MinValue
                };
            }
            return null;
        }

        /// <summary>Returns the most-recent password history entries for the given user, newest first.</summary>
        public async Task<List<MasterPasswordHistory>> GetLastPasswordHistoryAsync(int userId, int count)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = @"SELECT TOP (@count) Id, UserId, PasswordKey, AuthSalt, CreatedAt
                        FROM MasterPasswordHistory
                        WHERE UserId = @uid
                        ORDER BY CreatedAt DESC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@count", SqlDbType.Int).Value = count;
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = userId;
            using var reader = await cmd.ExecuteReaderAsync();
            var result = new List<MasterPasswordHistory>();
            while (await reader.ReadAsync())
            {
                result.Add(new MasterPasswordHistory
                {
                    Id = (int)reader["Id"],
                    UserId = (int)reader["UserId"],
                    PasswordKey = reader["PasswordKey"].ToString(),
                    AuthSalt = reader["AuthSalt"].ToString(),
                    CreatedAt = reader["CreatedAt"] != DBNull.Value ? (DateTime)reader["CreatedAt"] : DateTime.MinValue
                });
            }
            return result;
        }

        /// <summary>
        /// Atomically updates user credentials, archives the old password key, and re-encrypts all
        /// vault items in a single SQL transaction.  A server crash at any point between the three
        /// steps rolls back the whole unit so the database is never left in an inconsistent state.
        /// </summary>
        public async Task<bool> UpdateUserAndVaultAsync(User user, string oldPasswordKey, string oldAuthSalt,
            DateTime archivedAt, List<VaultItem> reEncryptedItems, int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Update user credentials.
                var userSql = @"UPDATE Users
                                SET Username = @name, Email = @email,
                                    MasterPasswordKey = @key, AuthSalt = @asalt, EncryptionSalt = @esalt,
                                    LastPasswordUpdate = GETDATE()
                                WHERE Id = @uid";
                using (var cmd = new SqlCommand(userSql, conn, transaction))
                {
                    cmd.Parameters.Add("@uid", SqlDbType.Int).Value = user.Id;
                    cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = user.Username;
                    cmd.Parameters.Add("@email", SqlDbType.NVarChar).Value = user.Email;
                    cmd.Parameters.Add("@key", SqlDbType.NVarChar).Value = user.MasterPasswordKey;
                    cmd.Parameters.Add("@asalt", SqlDbType.NVarChar).Value = user.AuthSalt;
                    cmd.Parameters.Add("@esalt", SqlDbType.NVarChar).Value = user.EncryptionSalt;
                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }

                // 2. Archive the old password key.
                var historySql = @"INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
                                   VALUES (@uid, @key, @salt, @date)";
                using (var cmd = new SqlCommand(historySql, conn, transaction))
                {
                    cmd.Parameters.Add("@uid", SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@key", SqlDbType.NVarChar).Value = oldPasswordKey;
                    cmd.Parameters.Add("@salt", SqlDbType.NVarChar).Value = oldAuthSalt;
                    cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = archivedAt;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Re-encrypt vault items.
                foreach (var item in reEncryptedItems)
                {
                    var vaultSql = @"UPDATE VaultItems
                                     SET IV = @iv, Tag = @tag, CipherText = @cipher
                                     WHERE Id = @id AND UserId = @uid";
                    using var cmd = new SqlCommand(vaultSql, conn, transaction);
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
