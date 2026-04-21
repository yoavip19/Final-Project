using System.Threading.Tasks;
using SecurioModels.DataTransferObjects;

namespace SecurioClient.Helpers.ServerHelpers
{
    // Calls the PasswordCheck endpoint used by the background worker to poll for password-health issues.
    public class PasswordCheckService : BaseService
    {
        // Sends the user's ID to the server and receives breach/old/master-old counts.
        public async Task<(bool Success, string Message, PasswordCheckResult Data)> CheckAsync(int userId)
        {
            var result = await PostAsync<PasswordCheckResult>("PasswordCheck", new { UserId = userId });
            return (result.Success, result.Message, result.Data);
        }
    }
}
