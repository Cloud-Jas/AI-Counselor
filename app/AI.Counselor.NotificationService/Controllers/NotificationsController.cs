using AI.Counselor.NotificationService.Models;
using AI.Counselor.NotificationService.Services;
using AI.Counselor.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace AI.Counselor.NotificationService.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IPostmarkServiceClient _postmarkServiceClient;

        public NotificationsController(IConfiguration configuration,IPostmarkServiceClient postmarkServiceClient)
        {
            _configuration = configuration;
            _postmarkServiceClient = postmarkServiceClient;
        }

        [HttpPost]
        [Route("/notifications/email/counsel")]
        public async Task<IActionResult> SendEmailNotification([FromBody] Summary summary)
        {
            try
            {
                string from = "admin@iamdivakarkumar.com";
                string to = "admin@iamdivakarkumar.com";
                string subject = "Your AI Counselor Update";                
                string counselingSummary = summary.CounselingSummary;
                string htmlTemplate = @"<html><head><title>Your AI Counselor Update</title><style>body {font-family: 'Arial', sans-serif;background-color: #f4f4f4;color: #333;margin: 0;padding: 0;}.container {max-width: 600px;margin: 20px auto;background-color: #fff;padding: 20px;border-radius: 8px;box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);}h1 {color: #007BFF;}p {line-height: 1.6;}.summary {background-color: #e9f5e6;padding: 10px;margin-top: 20px;border-radius: 8px;}.action-points {margin-top: 20px;}.action-points h2 {color: #007BFF;}.action-points ul {list-style-type: none;padding: 0;}.action-points li {margin-bottom: 10px;}.footer {margin-top: 20px;text-align: center;color: #555;}.blockquote {padding: 60px 80px 40px;position: relative;}.blockquote p {font-family: 'Utopia-italic';font-size: 35px;font-weight: 700px;text-align: center;}.blockquote:before {position: absolute;font-family: 'FontAwesome';top: 0;content:'\f10d';font-size: 200px;color: rgba(0,0,0,0.1);}.blockquote::after {content: '';top: 20px;left: 50%;margin-left: -100px;position: absolute;border-bottom: 3px solid #bf0024;height: 3px;width: 200px;}</style></head><body><div class='container'><h1>Your AI Counselor Update</h1><p>Hello Divakar,</p><div class='summary'>{{content}}</div><p>Thank you for using AI Counselor to enhance your well-being!</p><div class='footer'><p>Best regards,<br>Your AI Counselor Team</p></div></div></body></html>";
                string emailContent = htmlTemplate.Replace("{{content}}", counselingSummary);
                await _postmarkServiceClient.SendEmail(new PostmarkEmail
                {
                    From = from,
                    Subject = subject,
                    To = to,
                    HtmlBody = emailContent,
                });

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Failed to send notification." });
            }
        }


        [HttpPost]
        [Route("/notifications/sms/emotions")]
        public async Task<IActionResult> SendSMSNotification([FromBody] List<Emotion> emotions)
        {
            try
            {
                TwilioClient.Init(_configuration.GetValue<string>("Twilio:AccountSid"), _configuration.GetValue<string>("Twilio:AuthToken"));

                var message = await MessageResource.CreateAsync(
                    body: MessageTemplate(),
                    from: new Twilio.Types.PhoneNumber(_configuration.GetValue<string>("Twilio:TwilioPhoneNumber")),
                    to: new Twilio.Types.PhoneNumber(_configuration.GetValue<string>("Twilio:ToPhoneNumber"))
                );

                var messageSid = message.Sid;

                return Ok(new { MessageSid = messageSid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Failed to send notification." });
            }
        }

        private string MessageTemplate()
        {
            string messageBody = $@"Hello ,

We hope this message finds you well. We wanted to inform you that AI Counselor has tracked one of your emotions that need your attention!

Visit our website at https://aicounsellorfrontend.victoriousfield-921136ee.centralindia.azurecontainerapps.io/ and provide your inputs.

Thank you for choosing AI Counselor. We appreciate your business!

Best regards,
AICounselor Team";

            return messageBody;

        }
    }
}
