using AI.Counselor.Shared.Models;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text;

namespace AICounsellorFrontend
{
    public class CounselorServiceClient(HttpClient client)
    {
        public async Task<EmotionResponse> GetEmotionsAsync()
        {
            var response = await client.GetFromJsonAsync<EmotionResponse>("/emotions");

            return response;
        }
        public async Task UpdateEmotionReasonAsync(string id, List<Emotion> emotions)
        {
            var response = await client.PutAsJsonAsync<List<Emotion>>($"/emotions/{id}", emotions);

            response.EnsureSuccessStatusCode();
        }
    }
}
