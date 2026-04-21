using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using SecurioModels;
using Android.Security.Identity;
using SecurioModels.DataTransferObjects;

namespace SecurioClient.Helpers
{
    // The central engine for HTTP communication that handles JSON serialization and validates the success of every API call.
    public abstract class BaseService
    {
        protected static readonly HttpClient Client = new HttpClient();

        /// Update this to your local or Azure URL
        protected const string BaseUrl = "http://10.0.2.2:7071/api/";

        public BaseService()
        {
            if (Client.BaseAddress == null)
                Client.BaseAddress = new Uri(BaseUrl);
        }

        // This method now returns the specific response object directly.
        protected async Task<ServerResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                var jwt = await StorageHelper.GetJwt();
                if (!string.IsNullOrEmpty(jwt))
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");

                var response = await Client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Return the actual BaseResponse<T> from the server
                    return JsonConvert.DeserializeObject<ServerResponse<T>>(json);
                }

                // If server returns 401/500, try to parse the error message
                var error = JsonConvert.DeserializeObject<ServerResponse<T>>(json);
                return error ?? new ServerResponse<T> { Success = false, Message = "Server error occurred." };
            }
            catch
            {
                // If the internet is down, we return a new instance with the error
                return new ServerResponse<T> { Success = false, Message = $"Connection failed." };
            }
        }

        // Generic GET: Returns BaseResponse<T> containing the data
        protected async Task<ServerResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

                var jwt = await StorageHelper.GetJwt();
                if (!string.IsNullOrEmpty(jwt))
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");

                var fullUrl = new Uri(Client.BaseAddress, endpoint);
                Console.WriteLine($"QUAKE! DEBUG: Full Request URL: {fullUrl}!!!");

                var response = await Client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<ServerResponse<T>>(json);

                // Return the server's error message mapped to the object
                var error = JsonConvert.DeserializeObject<ServerResponse<T>>(json);
                return error ?? new ServerResponse<T> { Success = false, Message = "Could not retrieve data." };
            }
            catch (Exception)
            {
                return new ServerResponse<T> { Success = false, Message = "Network error." };
            }
        }
    }
}

