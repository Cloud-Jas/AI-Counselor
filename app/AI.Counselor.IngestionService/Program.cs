using AI.Counselor.IngestionService.BackgroundServices;
using AI.Counselor.IngestionService.Services;
using Azure;
using Azure.AI.OpenAI;
using System.Net.Http.Headers;

namespace AI.Counselor.IngestionService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();
            builder.Services.AddHostedService<Worker>();
            builder.AddAzureServiceBus("ServiceBusConnection");
            builder.Services.AddHttpClient<CounselorServiceClient>(static client => {
                client.BaseAddress = new("http://counselorservice");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
            builder.Services.AddHttpClient<NotificationServiceClient>(static client => {
                client.BaseAddress = new("http://notificationservice");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
            builder.Services.AddSingleton<OpenAIServiceClient>();
            builder.Services.AddSingleton(sp =>
            {
                var endpoint = new Uri(builder.Configuration.GetValue<string>("OpenAI:Endpoint"));
                var credential = new AzureKeyCredential((builder.Configuration.GetValue<string>("OpenAI:ApiKey")));
                var openAIClient = new OpenAIClient(endpoint, credential);

                return openAIClient;
            });
            // Add services to the container.

            builder.Services.AddControllers();

            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
