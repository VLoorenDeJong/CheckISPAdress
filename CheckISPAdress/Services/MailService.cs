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
                ConfigErrorReportModel report = new();

                mailConfigured = ConfigHelpers.MandatoryConfigurationChecks(_applicationSettingsOptions, _logger);
                if (mailConfigured) CreateBasicMailMessage();
                if (mailConfigured) report = ConfigHelpers.DefaultSettingsHaveBeenChanged(_applicationSettingsOptions, _logger);

                if (!(report.ChecksPassed))
                {
                    string emailBody = CreateEmail(report?.ErrorMessage!);

                    SendEmail(emailBody, "CheckISPAdress: Appsettings needs more configuration");
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
            message.Priority = MailPriority.High;
        }

        public void SendEmail(string emailBody, string subject)
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


                    message.Subject = subject;
                    message.Body = emailBody;
                    message.IsBodyHtml = true;

                    try
                    {
                        // Send the email message
                        client.Send(message);
                    }
                    catch (System.Net.Mail.SmtpException ex)
                    {
                        Type exceptionType = ex.GetType();
                        _logger.LogError("Email account password might be wrong. Exception type: {exceptionType}  Message:{message}", exceptionType, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Type exceptionType = ex.GetType();
                        _logger.LogError("Something went wrong with sending the email. Exception type: {exceptionType} Message:{message}", exceptionType, ex.Message);
                    }

                }

            }
        }

        public string CreateEmail(string emailMessage)
        {
            string outputMessage = "<html>"
                                     + "<head>"
                                        + "<style>"
                                             + "h1, h3, h4, h5, p { color: #666; font-family: Segoe UI; }"
                                             + "p { color: #666; font-family: Segoe UI; }"
                                         + "</style>"
                                     + "</head>"
                                     + "<body>"
                                     + $"{emailMessage}"
                                     + "</body>"
                                 + "</html>";

            return outputMessage;
        }

        public string HeartBeatEmail()
        {


            //string ISPAddressChangedEmail(string newISPAddress, double interval, int requestCounter, int checkCounter);
            string emailBody = string.Empty;

            return emailBody;
        }
    }
}
