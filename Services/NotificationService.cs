using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HazelnutVeb.Services
{
    public class NotificationService
    {
        private const string AppId = "76c8b428-a07d-4e09-9b01-1497eed30586";
        private const string RestApiKey = "76c8b428-a07d-4e09-9b01-1497eed30586";
        private const string ApiUrl = "https://onesignal.com/api/v1/notifications";

        public async Task SendLowInventoryNotification(double quantity)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", RestApiKey);

                var payload = new
                {
                    app_id = AppId,
                    included_segments = new[] { "All" },
                    headings = new { en = "Low Inventory Alert" },
                    contents = new { en = $"Only {quantity} kg left in stock!" }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await client.PostAsync(ApiUrl, content);
            }
        }
    }
}
