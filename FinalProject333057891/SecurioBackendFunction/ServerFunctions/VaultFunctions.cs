using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurioModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecurioBackendFunction.ServerFunctions
{
    public class VaultFunctions
    {
        [Function("AddVaultEntry")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("AddVaultEntry");

            // 1. Read the JSON from the Xamarin App
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var entry = JsonConvert.DeserializeObject<VaultItem>(requestBody);

            // 2. Get the Connection String we set in Step 2
            string connString = Environment.GetEnvironmentVariable("SqlConnectionString");

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    await conn.OpenAsync();

                    // 3. Prepare the SQL Command
                    var sql = @"INSERT INTO VaultEntries (UserId, AppName, AppUser, EncryptedPassword, Iv, Tag) 
                                VALUES (@uid, @name, @user, @pass, @iv, @tag)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", 1); // For now, hardcode User 1
                        cmd.Parameters.AddWithValue("@name", entry.AppName);
                        cmd.Parameters.AddWithValue("@user", entry.AppUsername);
                        cmd.Parameters.AddWithValue("@pass", entry.Ciphertext);
                        cmd.Parameters.AddWithValue("@iv", entry.Iv);
                        cmd.Parameters.AddWithValue("@tag", entry.Tag);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return new OkObjectResult("Successfully saved to Azure SQL");
            }
            catch (Exception ex)
            {
                logger.LogError($"Database Error: {ex.Message}");
                return new BadRequestObjectResult("Failed to save to database.");
            }
        }
    }
}
