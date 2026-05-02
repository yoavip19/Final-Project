using Android.Util;
using Newtonsoft.Json;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SecurioClient.Helpers.ServerHelpers
{
    /// <summary>Contacts the backend PasswordCheck endpoint on behalf of the background monitor service using only the stored UserId.</summary>
    public static class PasswordCheckService
    {
        private const string Tag = "PasswordCheckService";
        private static readonly HttpClient _http = new HttpClient();
        private const string Endpoint = "https://ironi-alef-cs-security-hkeacye5c5cchgg4.israelcentral-01.azurewebsites.net/api/PasswordCheck";

        /// <summary>Calls POST /api/PasswordCheck with the given userId; returns null on failure.</summary>
        public static async Task<PasswordCheckResult?> FetchAsync(int userId)
        {
            try
            {
                Log.Info(Tag, $"POST {Endpoint} for userId={userId}");
                var payload = JsonConvert.SerializeObject(new PasswordCheckRequest { UserId = userId });
                using var content  = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _http.PostAsync(Endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Warn(Tag, $"Server returned HTTP {(int)response.StatusCode} — returning null");
                    return null;
                }

                var json   = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ServerResponse<PasswordCheckResult>>(json);

                if (result?.Success != true)
                {
                    Log.Warn(Tag, $"Server response Success=false or null — message: {result?.Message}");
                    return null;
                }

                Log.Info(Tag, "Password check fetch succeeded");
                return result.Data;
            }
            catch (Exception ex)
            {
                // Fail open: a connectivity issue must not produce false positives.
                Log.Warn(Tag, $"Exception during password check fetch: {ex.Message}");
                return null;
            }
        }
    }
}
