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
    public class UserRepository
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
            var sql = "SELECT Id, Username, MasterPasswordKey FROM Users WHERE Email = @email";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@email", SqlDbType.NVarChar).Value = email;
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User { Id = (int)reader["Id"], Username = reader["Username"].ToString(), MasterPasswordKey = reader["MasterPasswordKey"].ToString() };
            }
            return null;
        }

        // UserRepository.cs - Fully populated profile query
        public async Task<User> GetUserProfileAsync(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"SELECT Id, Username, Email, CreatedAt, LastLogin, LastPasswordUpdate
                FROM Users WHERE Id = @uid"; ///Add //, (SELECT COUNT(*) FROM VaultItems WHERE UserId = @uid) AS PasswordCount // to get the count of passwords in the vault for this user

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
                    PasswordCount = 0
                    ///PasswordCount = (int)reader["PasswordCount"]
                };
            }
            return null;
        }
    }
}
