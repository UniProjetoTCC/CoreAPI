using Business.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Business.Services
{
    public class EmailSenderService : IEmailSenderService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;
        private readonly string _projectName;
        private readonly ILogger<EmailSenderService> _logger;

        public EmailSenderService(IConfiguration configuration, ILogger<EmailSenderService> logger)
        {
            _logger = logger;
            _projectName = configuration["ProjectName"] ??
                throw new InvalidOperationException("ProjectName configuration is not set in appsettings.json!");
            _smtpHost = configuration["EmailSettings:SmtpHost"] ??
                throw new InvalidOperationException("SmtpHost configuration is not set in appsettings.json!");
            _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"] ?? 
                throw new InvalidOperationException("SmtpPort configuration is not set in appsettings.json!"));
            _smtpUser = Environment.GetEnvironmentVariable("EMAIL_USER") ?? 
                throw new InvalidOperationException("EMAIL_USER environment variable is not set!");
            _smtpPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? 
                throw new InvalidOperationException("EMAIL_PASSWORD environment variable is not set!");
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                    EnableSsl = true,
                    Timeout = 10000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUser, _projectName),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                _logger.LogInformation("Sending email to {Email} with subject: {Subject}", email, subject);
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}. Error: {Error}", email, ex.Message);
                throw new ApplicationException("Failed to send email. Please try again later.", ex);
            }
        }
    }
}
