

using AI.Counselor.Shared.Models;
using System.Net.Http.Json;

namespace AI.Counselor.IngestionService.Services
{
    public class NotificationServiceClient(HttpClient client)
    {
        public async Task<HttpResponseMessage> NotifyEmotionAsync(List<Emotion> emotions)
        {
            var response = await client.PostAsJsonAsync<List<Emotion>>("/notifications/sms/emotions",emotions);

            return response;
        }

        public async Task<HttpResponseMessage> NotifyEmotionSummaryAsync(string counselingSummary)
        {
            var response = await client.PostAsJsonAsync<Summary>("/notifications/email/counsel", new Summary { CounselingSummary = counselingSummary });

            return response;
        }        
    }
}
