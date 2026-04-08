using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctionsProject
{
    public class TaskApi
    {
        // PASTE YOUR CONNECTION STRING HERE
        private readonly string _connString = Environment.GetEnvironmentVariable("SqlConnectionString");

        [Function("SaveTask")]
        public async Task<HttpResponseData> SaveTask([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string desc = query["desc"] ?? "Empty Task";

            using SqlConnection conn = new SqlConnection(_connString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("INSERT INTO MyTasks (TaskDescription) VALUES (@desc)", conn);
            cmd.Parameters.AddWithValue("@desc", desc);
            await cmd.ExecuteNonQueryAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Success! Saved: {desc}");
            return response;
        }

        [Function("ListTasks")]
        public async Task<HttpResponseData> ListTasks([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var tasks = new List<string>();

            using SqlConnection conn = new SqlConnection(_connString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT TaskDescription FROM MyTasks", conn);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tasks.Add(reader.GetString(0));
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(tasks);
            return response;
        }
    }
}
