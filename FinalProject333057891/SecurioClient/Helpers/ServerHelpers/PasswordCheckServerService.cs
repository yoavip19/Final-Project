using Newtonsoft.Json;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SecurioClient.Helpers.ServerHelpers
{
    // Contacts the backend PasswordCheck endpoint on behalf of the background monitor service.
    // No JWT is required: the service passes only the stored UserId.
    public static class PasswordCheckServerService
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string Endpoint = "http://10.0.2.2:7071/api/PasswordCheck";

        // Calls POST /api/PasswordCheck with the given userId.
        // Returns null if the server is unreachable or returns a non-success response,
        // so the caller can skip notification on transient failures (fail-open).
        public static async Task<PasswordCheckResult?> FetchAsync(int userId)
        {
            try
            {
                var payload = JsonConvert.SerializeObject(new PasswordCheckRequest { UserId = userId });
                using var content  = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _http.PostAsync(Endpoint, content);

                if (!response.IsSuccessStatusCode) return null;

                var json   = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ServerResponse<PasswordCheckResult>>(json);
                return result?.Success == true ? result.Data : null;
            }
            catch
            {
                // Fail open: a connectivity issue must not produce false positives.
                return null;
            }
        }
    }
}
