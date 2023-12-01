using AI.Counselor.NotificationService.Models;
using System.Text;

namespace AI.Counselor.NotificationService.Services
{
    public interface IPostmarkServiceClient
    {
        public Task SendEmail(PostmarkEmail postmarkEmail);
    }
    public class PostmarkServiceClient : IPostmarkServiceClient
    {        
        private readonly HttpClient _httpClient;

        public PostmarkServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendEmail(PostmarkEmail postmarkEmail)
        {            
            string postmarkApiUrl = "https://api.postmarkapp.com/email";
            string postData = $"{{\"From\":\"{postmarkEmail.From}\",\"To\":\"{postmarkEmail.To}\",\"Subject\":\"{postmarkEmail.Subject}\",\"HtmlBody\":\"{postmarkEmail.HtmlBody}\"}}";
            var content = new StringContent(postData, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(postmarkApiUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to send email. Status code: {response.StatusCode}. Response body: {responseBody}");
            }
        }
    }
}
