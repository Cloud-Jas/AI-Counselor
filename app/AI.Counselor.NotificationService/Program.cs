
using Twilio.Clients;
using Twilio;
using AI.Counselor.NotificationService.Services;

namespace AI.Counselor.NotificationService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton<TwilioRestClient>(sp => new TwilioRestClient(builder.Configuration["Twilio:AccountSid"],builder.Configuration["Twilio:AuthToken"]));
            builder.Services.AddHttpClient<IPostmarkServiceClient, PostmarkServiceClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.postmarkapp.com");
                client.DefaultRequestHeaders.Add("Accept", "application/json");                
                client.DefaultRequestHeaders.Add("X-Postmark-Server-Token", builder.Configuration["Postmark:ServerToken"]);
            });
            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
