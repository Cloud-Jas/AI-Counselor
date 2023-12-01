using AI.Counselor.Shared.Models;
using System.Net.Http.Json;

namespace AI.Counselor.IngestionService.Services
{
    public class CounselorServiceClient(HttpClient client)
    {
        public async Task<HttpResponseMessage> PostEmotionsAsync(EmotionData EmotionData)
        {            
            var response = await client.PostAsJsonAsync<EmotionData>("/emotions", EmotionData);

            return response;
        }
        public async Task<EmotionResponse> GetEmotionsAsync()
        {
            var response = await client.GetFromJsonAsync<EmotionResponse>("/emotions");

            return response;
        }
    }
}
