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
    // Manages all direct SQL database interactions for user-related data.
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;
        public UserRepository(string connectionString) => _connectionString = connectionString;

        // Checks the database to see if a specific email address is already in use.
        public async Task<bool> EmailExistsAsync(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = "SELECT COUNT(1) FROM Users WHERE Email = @email";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@email", SqlDbType.NVarChar).Value = email;
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        // Inserts a new user record and returns the newly generated ID.
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

        // Retrieves a user record by email for authentication purposes.
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


        // UserRepository.cs - Fully populated profile query
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
        // Updates the LastLogin timestamp for the given user to the current UTC time.
        public async Task UpdateLastLoginAsync(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = "UPDATE Users SET LastLogin = GETDATE() WHERE Id = @uid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@uid", SqlDbType.Int).Value = userId;
            await cmd.ExecuteNonQueryAsync();
        }

        // Checks whether the email is already used by a different user (excludes the given userId).
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

        // Updates the user's profile fields. When passwordChanged is true, also updates
        // MasterPasswordKey, AuthSalt, EncryptionSalt, and sets LastPasswordUpdate.
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

        // Deletes the user account. CASCADE on VaultItems removes all associated passwords automatically.
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
    }
}
