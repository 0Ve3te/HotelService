using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace HotelService.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            string fromMail = _config["ConnectionEmail:EmailFrom"];
            string fromPassword = _config["ConnectionEmail:EmailPassword"];

            MailMessage mailMessage = new MailMessage();
            mailMessage.Subject = "Apartament.pl - " + subject;
            mailMessage.Body = "<html><body>"+htmlMessage+"</body></html>";
            mailMessage.IsBodyHtml = true;
            mailMessage.From = new MailAddress(fromMail);
            mailMessage.To.Add(new MailAddress(email));

            SmtpClient client = new SmtpClient
            {
                Port = Convert.ToInt32(_config["ConnectionEmail:Port"]),
                Host = _config["ConnectionEmail:Host"],
                EnableSsl = Convert.ToBoolean(_config["ConnectionEmail:EnableSsl"]),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = Convert.ToBoolean(_config["ConnectionEmail:UseDefaultCredentials"]),
                Credentials = new NetworkCredential(fromMail, fromPassword)

            };

            return client.SendMailAsync(mailMessage);
        }
    }
}
