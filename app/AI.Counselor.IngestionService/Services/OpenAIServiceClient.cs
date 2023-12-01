using Azure.AI.OpenAI;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;

namespace AI.Counselor.IngestionService.Services
{
    public class OpenAIServiceClient
    {
        private readonly OpenAIClient _openAiClient;
        private readonly string _deploymentId;
        private readonly int _maxTokens;
        private readonly float _temperature;
        public OpenAIServiceClient(OpenAIClient openAiClient,IConfiguration configuration)
        {
            _openAiClient = openAiClient;
            _deploymentId = configuration.GetValue<string>("OpenAI:DeploymentId");
            _maxTokens = configuration.GetValue<int>("OpenAI:Prompt:MaxTokens");
            _temperature = configuration.GetValue<float>("OpenAI:Prompt:Temperature");
        }
        public async Task<string> GetCounselingSummary(string emotionsTimeSeries)
        {
            try
            {
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _deploymentId,
                    Messages =
                {
                    new ChatMessage(ChatRole.System, "System"),
                    new ChatMessage(ChatRole.System, $"Act as an expert counselor and provide me counseling summary based on the series of emotions that I went through this day, where timestamp start and end is in unixtimestamp format." +
                    $" If you want to specify time period in this glimpse of summary do specify time period in human readable date format (not in timestamp). Provide summary along with some possible explanations to it, if reason is not explicitly provided. " +
                    $"Feel free to ignore emotions that are not to be concerned about, like neutral emotions. Also provide the summary in a email friendly format with <hr> <b> <br/> tags wherever relevant. " +
                    $"If needed, provide me some action points for me to follow with <ul> <li> tags. End the summary with a famous quote that will be relevant for my summary, use a different style and border for quotes." +
                    $"Here is the emotion: \\n"),
                    new ChatMessage(ChatRole.User, emotionsTimeSeries),
                },
                    MaxTokens = _maxTokens,
                    Temperature = _temperature,
                };

                var result = await this._openAiClient
                                       .GetChatCompletionsAsync(chatCompletionsOptions)
                                       .ConfigureAwait(false);
                var summary = result.Value.Choices[0].Message.Content;
                string pattern = @"[\r\n""]";
                return Regex.Replace(summary, pattern, "");
            }
            catch(Exception ex) 
            {
                return default;
            }
        }
    }
}
