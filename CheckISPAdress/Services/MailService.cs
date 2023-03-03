using CheckISPAdress.Helpers;
using CheckISPAdress.Interfaces;
using CheckISPAdress.Models;
using CheckISPAdress.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using static CheckISPAdress.Options.ApplicationSettingsOptions;

namespace CheckISPAdress.Services
{
    public class MailService : IMailService
    {
        private readonly ILogger _logger;
        private readonly ApplicationSettingsOptions _applicationSettingsOptions;

        private MailMessage message = new MailMessage();

        public MailService(ILogger<CheckISPAddressService> logger, IOptions<ApplicationSettingsOptions> applicationSettingsOptions)
        {
            _logger = logger;
            _applicationSettingsOptions = applicationSettingsOptions!.Value;

            if (_applicationSettingsOptions is not null)
            {
                bool mailConfigured = true;
                ConfigErrorReportModel report = new ConfigErrorReportModel { ChecksPassed = true };

                mailConfigured = ConfigHelpers.DefaultMailSettingsChanged(_applicationSettingsOptions, _logger);
                if (mailConfigured) CreateBasicMailMessage();
                if (mailConfigured) report = ConfigHelpers.DefaultSettingsHaveBeenChanged(_applicationSettingsOptions, _logger);

                if (!(report.ChecksPassed))
                {
                    string completeMessage = "";
                    foreach (string message in report.ErrorMessages)
                    {
                        completeMessage = $"{completeMessage}, {message}";
                    }

                    SendEmail(completeMessage);
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private void CreateBasicMailMessage()
        {
            // Set the sender, recipient, subject, and body of the message
            message.From = new MailAddress(_applicationSettingsOptions.EmailFromAdress!);
            message.To.Add(new MailAddress(_applicationSettingsOptions.EmailToAdress!));
            message.Subject = _applicationSettingsOptions!.EmailSubject;
            message.Priority = MailPriority.High;
        }

        public void SendEmail(string emailBody)
        {
            if (_applicationSettingsOptions is not null)
            {
                // Create a new SmtpClient object within a using block
                using (SmtpClient client = new SmtpClient())
                {
                    // Configure the SMTP client with your email provider's SMTP server address and credentials
                    client.Host = _applicationSettingsOptions.MailServer!; ; // Replace with your SMTP server address
                    client.Port = _applicationSettingsOptions.SMTPPort; // Replace with your SMTP server port number
                    client.UseDefaultCredentials = _applicationSettingsOptions.UseDefaultCredentials; // If your SMTP server requires authentication, set this to false
                    client.Credentials = new NetworkCredential(_applicationSettingsOptions?.UserName, _applicationSettingsOptions?.Password); // Replace with your SMTP server username and password
                    client.EnableSsl = _applicationSettingsOptions!.EnableSsl; // Set this to true if your SMTP server requires SSL/TLS encryption               

                    message.Body = emailBody;
                    message.IsBodyHtml = true;

                    try
                    {
                        // Send the email message
                        client.Send(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Something went wrong with sending the email. Message:{message}", ex.Message);
                    }

                }

            }
        }

        public string ISPAddressChangedEmail(string newISPAddress, string oldISPAddress, double interval, int requestCounter, int checkCounter)
        {
            string hostingProviderText = _applicationSettingsOptions.DNSRecordHostProviderName!;
            if (string.Equals(hostingProviderText, StandardAppsettingsValues.DNSRecordHostProviderName, StringComparison.CurrentCultureIgnoreCase)) hostingProviderText = _applicationSettingsOptions.DNSRecordHostProviderURL!;

            string emailBody = "<html>"
                                     + "<head>"
                                        + "<style>"
                                             + "h1, h3, h4, h5, p { font-family: Segoe UI; }"
                                             + "p { color: #666; }"
                                         + "</style>"
                                     + "</head>"
                                     + "<body>"
                                         + $@"<p><strong> {newISPAddress} </strong> is your new ISP adress </p>"
                                         + $"<p>Go to <a href = '{_applicationSettingsOptions.DNSRecordHostProviderURL}'> <strong>{hostingProviderText}</strong> </a> to update the DNS record.</p>"
                                         + $"<p>I wish you a splendid rest of your day!</p>"
                                         + $"<p>Your API</p>"
                                         + $"<p><strong>Here are some statistics:</strong></p>"
                                         + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                                         + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions.DateTimeFormat)} </strong><p>"
                                         + $"<p>API Calls: <strong> {requestCounter} </strong><p>"
                                         + $"<p>Script runs: <strong> {checkCounter} </strong><p>"
                                     + "</body>"
                                 + "</html>";

            return emailBody;
        }
    }
}
