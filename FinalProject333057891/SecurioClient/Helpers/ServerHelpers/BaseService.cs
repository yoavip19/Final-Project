using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using SecurioModels;
using Android.Security.Identity;
using SecurioModels.Responses;

namespace SecurioClient.Helpers
{
    // The central engine for HTTP communication that handles JSON serialization and validates the success of every API call.
    public abstract class BaseService
    {
        protected readonly HttpClient Client = new HttpClient();

        /// Update this to your local or Azure URL
        protected const string BaseUrl = "http://10.0.2.2:7071/api/";

        public BaseService()
        {
            Client.BaseAddress = new Uri(BaseUrl);
        }

        // This method now returns the specific response object directly.
        protected async Task<BaseResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await Client.PostAsync(endpoint, content);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Return the actual BaseResponse<T> from the server
                    return JsonConvert.DeserializeObject<BaseResponse<T>>(json);
                }

                // If server returns 401/500, try to parse the error message
                var error = JsonConvert.DeserializeObject<BaseResponse<T>>(json);
                return error ?? new BaseResponse<T> { Success = false, Message = "Server error occurred." };
            }
            catch (Exception ex)
            {
                // If the internet is down, we return a new instance with the error
                return new BaseResponse<T> { Success = false, Message = $"Connection failed: {ex.Message}" };
            }
        }

        // Generic GET: Returns BaseResponse<T> containing the data
        protected async Task<BaseResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var fullUrl = new Uri(Client.BaseAddress, endpoint);
                Console.WriteLine($"QUAKE! DEBUG: Full Request URL: {fullUrl}!!!");

                var response = await Client.GetAsync(endpoint);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<T>>(json);

                // Return the server's error message mapped to the object
                var error = JsonConvert.DeserializeObject<BaseResponse<T>>(json);
                return error ?? new BaseResponse<T> { Success = false, Message = "Could not retrieve data." };
            }
            catch (Exception)
            {
                return new BaseResponse<T> { Success = false, Message = "Network error." };
            }
        }
    }
}

