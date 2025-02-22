using Business.Models;
using Business.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Collections.Generic;

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
        private readonly IMessageBrokerService _messageBroker;
        private readonly SemaphoreSlim _emailSemaphore = new SemaphoreSlim(1, 1);
        private readonly HashSet<string> _processingEmails = new HashSet<string>();
        private readonly object _lockObject = new object();

        public EmailSenderService(
            IConfiguration configuration, 
            ILogger<EmailSenderService> logger,
            IMessageBrokerService messageBroker)
        {
            _logger = logger;
            _messageBroker = messageBroker;
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

            InitializeMessageBroker();
        }

        private void InitializeMessageBroker()
        {
            _messageBroker.SubscribeAsync<EmailMessage>("email_notifications", async message =>
            {
                // Generate a unique key for this email
                var emailKey = $"{message.To}_{message.Subject}_{DateTime.UtcNow.Ticks}";

                // Check if this email is already being processed
                lock (_lockObject)
                {
                    if (_processingEmails.Contains(emailKey))
                    {
                        _logger.LogWarning("Duplicate email detected for {Recipient} with subject {Subject}", message.To, message.Subject);
                        return;
                    }
                    _processingEmails.Add(emailKey);
                }

                try
                {
                    await _emailSemaphore.WaitAsync();
                    _logger.LogInformation("Processing email for {Recipient}", message.To);
                    
                    using var client = new SmtpClient(_smtpHost, _smtpPort)
                    {
                        Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                        EnableSsl = true,
                        Timeout = 10000 // 10 seconds timeout
                    };

                    using var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_smtpUser, _projectName),
                        Subject = message.Subject,
                        Body = message.Body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(message.To);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("Email sent successfully to {Recipient}", message.To);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Recipient}", message.To);
                    throw;
                }
                finally
                {
                    _emailSemaphore.Release();
                    lock (_lockObject)
                    {
                        _processingEmails.Remove(emailKey);
                    }
                }
            }).GetAwaiter().GetResult();

            _logger.LogInformation("Email service initialized and subscribed to message broker");
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var emailMessage = new EmailMessage
                {
                    To = email,
                    Subject = subject,
                    Body = message
                };

                await _messageBroker.PublishAsync("email_notifications", emailMessage);
                _logger.LogInformation("Email queued for {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue email for {Email}. Error: {Error}", email, ex.Message);
                throw new ApplicationException("Failed to queue email. Please try again later.", ex);
            }
        }
    }
}
