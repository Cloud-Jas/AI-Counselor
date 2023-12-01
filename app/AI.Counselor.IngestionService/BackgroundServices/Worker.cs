using System.Text;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;
using AI.Counselor.Shared.Models;
using System.Runtime.InteropServices;
using System.Timers;
using AI.Counselor.IngestionService.Services;

namespace AI.Counselor.IngestionService.BackgroundServices
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly CounselorServiceClient _counselorServiceClient;
        private readonly NotificationServiceClient _notificationServiceClient;
        private readonly ServiceBusProcessor _serviceBusProcessor;
        private readonly OpenAIServiceClient _openAIServiceClient;
        private readonly IConfiguration _configuration;
        private System.Timers.Timer counselingServiceTimer = null;
        public Worker(ILogger<Worker> logger, ServiceBusClient serviceBusClient, IConfiguration configuration, CounselorServiceClient counselorServiceClient, NotificationServiceClient notificationServiceClient, OpenAIServiceClient openAIServiceClient)
        {
            _logger = logger;
            _configuration = configuration;
            var queueName = _configuration.GetValue<string>("ServiceBus:QueueName");
            _serviceBusProcessor = serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions());
            _counselorServiceClient = counselorServiceClient;
            _notificationServiceClient = notificationServiceClient;
            _openAIServiceClient = openAIServiceClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _serviceBusProcessor.ProcessMessageAsync += ProcessMessagesAsync;

            _serviceBusProcessor.ProcessErrorAsync += ExceptionReceivedHandler;

            await _serviceBusProcessor.StartProcessingAsync();

            counselingServiceTimer = new System.Timers.Timer(1000 * 60);
            
            counselingServiceTimer.Elapsed += new ElapsedEventHandler(OnTriggerEvent);
            
            counselingServiceTimer.Start();

            while (!stoppingToken.IsCancellationRequested)
            {                
                await Task.Delay(1000, stoppingToken);
            }

            await _serviceBusProcessor.StopProcessingAsync();
        }
        private void OnTriggerEvent(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Hour == 23 && DateTime.Now.Minute == 59)
            {
                Task.Run(async () => await ProvideCounselingAsync());
            }
        }
        private async Task ProvideCounselingAsync()
        {
            var emotions = await _counselorServiceClient.GetEmotionsAsync();

            var counselingSummary = await _openAIServiceClient.GetCounselingSummary(JsonConvert.SerializeObject(emotions));

            await _notificationServiceClient.NotifyEmotionSummaryAsync(counselingSummary);
            
        }
        private async Task ProcessMessagesAsync(ProcessMessageEventArgs args)
        {
            try
            {
                var messageBody = Encoding.UTF8.GetString(args.Message.Body);
                var emotionData = JsonConvert.DeserializeObject<EmotionData>(messageBody);

                if (emotionData != null && emotionData.Emotions != null)
                {
                    var response = await _counselorServiceClient.PostEmotionsAsync(emotionData);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Message sent successfully.");
                        await args.CompleteMessageAsync(args.Message);
                    }
                    else
                    {
                        _logger.LogError($"Failed to send message. StatusCode: {response.StatusCode}");
                        await args.AbandonMessageAsync(args.Message);
                    }

                    if (emotionData.Emotions.Any(emotion => IsGreaterThan10Seconds(emotion.TimestampRange)))
                    {
                        await _notificationServiceClient.NotifyEmotionAsync(emotionData.Emotions);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while processing the message: {ex.Message}");
                await args.AbandonMessageAsync(args.Message);
            }
        }
        public bool IsGreaterThan10Seconds(TimestampRange timestampRange)
        {
            var duration = TimeSpan.FromSeconds(timestampRange.End - timestampRange.Start);
            return duration.TotalSeconds > 10;
        }
        private Task ExceptionReceivedHandler(ProcessErrorEventArgs exceptionReceivedEventArgs)
        {
            _logger.LogError($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            return Task.CompletedTask;
        }
    }
}
