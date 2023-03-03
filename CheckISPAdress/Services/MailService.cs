using CheckISPAdress.Helpers;
using CheckISPAdress.Models;
using CheckISPAdress.Options;
using Microsoft.Extensions.Options;
using System.Drawing;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

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

                    // Send the email message
                    client.Send(message);

                }

            }
        }

        public string ISPAddressChangedEmail(string newISPAddress, string oldISPAddress, string? dNSRecordHostingProviderURL, double interval, int requestCounter, int checkCounter)
        {

           string something =     "<html>" 
                                    +"<head>"
                                       + "<style>"
                                            + "h1, p { font-family: Segoe UI; }"
                                            + "p { color: #666; }"
                                        + "</style>"
                                    + "</head>" 
                                    +"<body>"
                                        + $"<h1>Your isp adress is changed to {newISPAddress}</h1>"
                                        + $"<p>Go to <a href = '{dNSRecordHostingProviderURL}' > your hosting provider </a> To update the address.</p></p>"
                                    + "</body>" 
                                +"</html>";

            //string htmlBody = $"<html><head><style>body {font - family: 'Segoe UI', Tahoma, Verdana, Arial, sans - serif; font - size: 14px;}</style >
            //                   </head >
            //                   <body>
            //                     <h1> The ISP adres is changed </h1>
            //                     <p> .</p>
            //                     <p><a href = 'http://www.example.com' > Visit our website </a> for more information.</p>
            //                   </body>
            //                 </html>
            //                 ";


            return something;
        }
    }
}
