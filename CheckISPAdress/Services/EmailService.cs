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
    public class EmailService : IEmailService

    {
        private readonly ILogger _logger;
        private readonly ApplicationSettingsOptions _applicationSettingsOptions;

        private MailMessage message = new MailMessage();

        public EmailService(ILogger<CheckISPAddressService> logger, IOptions<ApplicationSettingsOptions> applicationSettingsOptions)
        {
            _logger = logger;
            _applicationSettingsOptions = applicationSettingsOptions!.Value;

            CreateBasicMailMessage();
        }

        private void CreateBasicMailMessage()
        {
            // Set the sender, recipient, subject, and body of the message
            message.From = new MailAddress(_applicationSettingsOptions.EmailFromAdress!);
            message.To.Add(new MailAddress(_applicationSettingsOptions.EmailToAdress!));
            message.Priority = MailPriority.High;
        }

        private string CreateEmail(string emailMessage)
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

        private void SendEmail(string emailBody, string subject)
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

        public void SendHeartBeatEmail(IISPAdressCounterService counterService, string oldISPAddress, string currentISPAddress, string newISPAddress, Dictionary<string, string> externalISPCheckResults)
        {
            string message = $@"<p><strong>This was fun! </strong></p>"
                                 + $"<p>API calls:<strong> {counterService.GetServiceRequestCounter()}</strong></p>"
                                 + $"<p>API call check: <strong>{counterService.GetServiceCheckCounter()}</strong></p>"
                                 + $"<p>Internal API calls: <strong>{counterService.GetInternalAPICallsCounter()}</strong></p>"
                                 + $"<p>External API calls: <strong>{counterService.GetExternalServiceCheckCounter()}</strong></p>"
                                 + $@"<p>Current ISP: <strong> {currentISPAddress}</strong></p>";
            foreach (KeyValuePair<string, string> ISPAdressCheck in externalISPCheckResults!)
            {
                
                string ispReport = $"<p>Backup API: <a href = '{ISPAdressCheck.Key}'> {ISPAdressCheck.Key} </a> -> <strong>{ISPAdressCheck.Value}</strong></p>";
                message = $"{message} {ispReport}";
            }
            message = $"{message} <p>TimeIntervalInMinutes: <strong>{_applicationSettingsOptions?.TimeIntervalInMinutes}</strong></p>"
                      + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                      + $@"<p>Old ISP: <strong> {oldISPAddress}</strong></p>"
                      + $@"<p>New ISP: <strong> {newISPAddress}</strong></p>"
                      + $"<p>See you in {_applicationSettingsOptions?.HeatbeatEmailIntervalDays} days ;)</p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "ISP address checker update");
        }

        public void SendCounterDifferenceEmail(IISPAdressCounterService counterService)
        {
            string message = $"<p>The ISP check counters are out of sync.</p>"
                              + $"<p>requestCounter : <strong>{counterService.GetServiceRequestCounter()}</strong></p>"
                              + $"<p>checkCounter : <strong>{counterService.GetServiceCheckCounter()}</strong></p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "CheckISPAddress: counter difference");
        }

        public void SendConfigErrorMail(string errorMessage)
        {
            string emailBody = CreateEmail(errorMessage);

            SendEmail(emailBody, "CheckISPAddress: configuration error");
        }

        public void SendConfigSuccessMail(IISPAdressCounterService counterService)
        {
            string message = $@"<p>You have succesfully configured this application.</p>"
                                  + "<p><strong>This was fun! </strong></p>"
                                  + $"<p>I wish you a splendid rest of your day!</p>"
                                  + $@"<br />"
                                  + $@"<br />"
                                  + $"<p><strong>The folowing things were configured:</strong></p>"
                                  + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                                  + $"<p>TimeIntervalInMinutes: <strong>{_applicationSettingsOptions?.TimeIntervalInMinutes}</strong></p>"
                                  + $"<p>Every week on <strong> {_applicationSettingsOptions?.HeatbeatEmailDayOfWeek} </strong> at <strong> {_applicationSettingsOptions?.HeatbeatEmailTimeOfDay} </strong> a E-mail will be send</p>"
                                  + $"<p>DNSRecordHostProviderName: <strong>{_applicationSettingsOptions?.DNSRecordHostProviderName}</strong></p>"
                                  + $"<p>DNSRecordHostProviderURL : <strong>{_applicationSettingsOptions?.DNSRecordHostProviderURL}</strong></p>"
                                  + $"<p>EmailFromAdress : <strong>{_applicationSettingsOptions?.EmailFromAdress}</strong></p>"
                                  + $"<p>EmailToAdress : <strong>{_applicationSettingsOptions?.EmailToAdress}</strong></p>"
                                  + $"<p>EmailSubject : <strong>{_applicationSettingsOptions?.EmailSubject}</strong></p>"
                                  + $"<p>MailServer : <strong>{_applicationSettingsOptions?.MailServer}</strong></p>"
                                  + $"<p>userName: <strong>{_applicationSettingsOptions?.UserName}</strong></p>"
                                  + $"<p>password : <strong>*Your password*</strong></p>"
                                  + $"<p>EnableSsl : <strong>{_applicationSettingsOptions?.EnableSsl}</strong></p>"
                                  + $"<p>SMTPPort : <strong>{_applicationSettingsOptions?.SMTPPort}</strong></p>"
                                  + $"<p>UseDefaultCredentials : <strong>{_applicationSettingsOptions?.UseDefaultCredentials}</strong></p>"
                                  + $"<p>DateTimeFormat : <strong>{_applicationSettingsOptions?.DateTimeFormat}</strong></p>";
            // Write out the list of API's
            if (_applicationSettingsOptions?.BackupAPIS is not null)
            {
                foreach (string? backupAPI in _applicationSettingsOptions?.BackupAPIS!)
                {
                    message = $"{message} " +
                                $"<p>Backup API {_applicationSettingsOptions?.BackupAPIS.IndexOf(backupAPI)} : <a href = '{backupAPI}'> <strong>{backupAPI}</strong> </a></p>";
                }
            }
            // Finish the email body.
            message = $"{message} "
                       + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                       + $"<p>API Calls: <strong> {counterService.GetServiceRequestCounter()} </strong><p>"
                       + $"<p>Script runs: <strong> {counterService.GetServiceCheckCounter()} </strong><p>"
                       + $"<p>Failed attempts counter: <strong> {counterService.GetFailedISPRequestCounter()} </strong><p>"
                       + $"<p>Endpoint calls: <strong> {counterService.GetInternalAPICallsCounter()} </strong><p>"
                       + $"<p>A call is made every <strong> {_applicationSettingsOptions!.TimeIntervalInMinutes} </strong>minutes<p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "ISPAdressChecker: Congratulations configuration succes!!");
        }

        public void SendConnectionReestablishedEmail(string newISPAddress, string oldISPAddress, IISPAdressCounterService counterService, double interval)
        {
            string message = $@"<p>ISP adress has changed and I found my self again.</p>"
                                 + $@"<p><strong> {newISPAddress} </strong> is your new ISP adress</p>"
                                 + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                                 + "<p><strong>This is fun, hope it goes this well next time! </strong></p>"
                                 + $"<p>I wish you a splendid rest of your day!</p>"
                                 + $"<p>Your API</p>"
                                 + $"<p><strong>Here are some statistics:</strong></p>"
                                 + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                                 + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                                 + $"<p>Failed attempts counter: <strong> {counterService.GetFailedISPRequestCounter()} </strong>(This counter is reset after this E-mail is send)<p>"
                                 + $"<p>External API calls: <strong>{counterService.GetExternalServiceCheckCounter()}</strong></p>"
                                 + $"<p>API Calls: <strong> {counterService.GetServiceRequestCounter()} </strong><p>"
                                 + $"<p>Script runs: <strong> {counterService.GetServiceCheckCounter()} </strong><p>"
                                 + $"<p>Endpoint calls: <strong> {counterService.GetInternalAPICallsCounter()} </strong><p>"
                                 + $"<p>The old ISP adrdess was: {oldISPAddress}<p>"
                                 + $"<p>API endpoint URL: <strong><a href{_applicationSettingsOptions?.APIEndpointURL}</strong></p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "ISPAdressChecker: ISP adress changed but I found my seld");
        }

        public void SendISPAPIHTTPExceptionEmail(string exceptionType, string exceptionMessage)
        {
            string message = $"<p>API Did not respond:</p>"
                           + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                           + "<p>exceptionType:</p>"
                           + $"<p><strong>{exceptionType}</strong></p>"
                           + "<p>message:</p>"
                           + $"<p><strong>{exceptionMessage}<strong></p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "CheckISPAddress: API endpoint HTTP exception");
        }

        public void SendISPAPIExceptionEmail(string exceptionType, string exceptionMessage)
        {
            _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, exceptionMessage);

            string message = $"<p>Exception fetching ISP address from API:</p>"
                           + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                           + "<p>exceptionType:"
                           + $"<p><strong>{exceptionType}</strong></p>"
                           + "<p>message:"
                           + $"<p><strong>{exceptionMessage}<strong></p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "CheckISPAddress: API Call error");
        }

        public void SendExternalAPIHTTPExceptionEmail(string APIUrl, string exceptionType, string exceptionMessage)
        {

            string message = $"<p>API Did not respond:</p>"
                            + $"<p><a href = '{APIUrl}'> <strong>{APIUrl}</strong> </a> </p>"                           
                            + "<p>exceptionType:</p>"
                            + $"<p><strong>{exceptionType}</strong></p>"
                            + "<p>message:</p>"
                            + $"<p><strong>{exceptionMessage}<strong></p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "CheckISPAddress: Backup API HTTP exception");
        }

        public void SendExternalAPIExceptionEmail(string APIUrl, string exceptionType, string exceptionMessage)
        {
            string message = $"<p>Exception fetching ISP address from API:</p>"
                            + $"<p><a href = '{APIUrl}'> <strong>{APIUrl}</strong> </a> </p>"
                           + "<p>exceptionType:"
                           + $"<p><strong>{exceptionType}</strong></p>"
                           + "<p>message:"
                           + $"<p><strong>{exceptionMessage}<strong></p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "CheckISPAddress: API Call error");
        }

        public void SendISPAdressChangedEmail(string externalISPAddress, string oldISPAddress, IISPAdressCounterService counterService, double interval)
        {
            // hostingProviderText is the link to the hostprovider, id specified is shows the name
            string hostingProviderText = string.Equals(_applicationSettingsOptions?.DNSRecordHostProviderName, StandardAppsettingsValues.DNSRecordHostProviderName, StringComparison.CurrentCultureIgnoreCase)? _applicationSettingsOptions?.DNSRecordHostProviderURL! : _applicationSettingsOptions?.DNSRecordHostProviderName!;

            string message = $@"<p><strong> {externalISPAddress} </strong> is your new ISP adress</p>"
                              + $"<p>Go to <a href = '{_applicationSettingsOptions?.DNSRecordHostProviderURL}'> <strong>{hostingProviderText}</strong> </a> to update the DNS record.</p>"
                              + $"<p>External API calls: <strong>{counterService.GetExternalServiceCheckCounter()}</strong></p>"
                              + $"<p>I wish you a splendid rest of your day!</p>"
                              + $"<p>Your API</p>"
                              + $"<p><strong>Here are some statistics:</strong></p>"
                              + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                              + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                              + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                              + $"<p>Failed attempts counter: <strong> {counterService.GetFailedISPRequestCounter()} </strong><p>"
                              + $"<p>API Calls: <strong> {counterService.GetServiceRequestCounter()} </strong><p>"
                              + $"<p>Script runs: <strong> {counterService.GetServiceCheckCounter()} </strong><p>"
                              + $"<p>Endpoint calls: <strong> {counterService.GetInternalAPICallsCounter()} </strong><p>"
                              + $"<p>The old ISP adrdess was:<p>"
                              + $"<p>{oldISPAddress}<p>"
                              ;

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, _applicationSettingsOptions?.EmailSubject!);
        }

        public void SendDifferendISPAdressValuesEmail(Dictionary<string, string> externalISPAdressChecks, string oldISPAddress, IISPAdressCounterService counterService, double interval)
        {
            string message = $@"<p><strong> Multiple </strong> ISP adresses returned</p>";

            foreach (KeyValuePair<string, string> ISPAdressCheck in externalISPAdressChecks!)
            {
                string ispReport = $"<p><a href = '{ISPAdressCheck.Key}'>{ISPAdressCheck.Key}</a> - <strong>{ISPAdressCheck.Value}</strong></p>";
                message = $"{message} {ispReport}";
            }

            message = $"{message}"
                        + "<p><strong>Best of luck solving this one!</strong></p>"
                        + $"<p>I wish you a splendid rest of your day!</p>"
                        + $"<p>Your API</p>" + $"<p><strong>Here are some statistics:</strong></p>"
                        + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                        + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                        + $"<p>Failed attempts counter: <strong> {counterService.GetFailedISPRequestCounter} </strong><p>"
                        + $"<p>API Calls: <strong> {counterService.GetServiceRequestCounter()} </strong><p>"
                        + $"<p>Script runs: <strong> {counterService.GetServiceCheckCounter()} </strong><p>"
                        + $"<p>Endpoint calls: <strong> {counterService.GetInternalAPICallsCounter()} </strong><p>"
                        + $"<p>The old ISP adrdess was:<p>"
                        + $"<p>{oldISPAddress}<p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "ISPAdressChecker: multiple ISP adresses were returned");
        }

        public void SendNoISPAdressReturnedEmail(string oldISPAddress, IISPAdressCounterService counterService, double interval)
        {

            string message = $@"<p>No adresses were returned are there any exceptions?</p>"
                        + "<p><strong>Best of luck solving this one!</strong></p>"
                        + $"<p>I wish you a splendid rest of your day!</p>"
                        + $"<p>Your API</p>" + $"<p><strong>Here are some statistics:</strong></p>"
                        + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                        + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions.DateTimeFormat)} </strong><p>"
                        + $"<p>Failed attempts counter: <strong> {counterService.GetFailedISPRequestCounter()} </strong><p>"
                        + $"<p>API Calls: <strong> {counterService.GetServiceRequestCounter()} </strong><p>"
                        + $"<p>Script runs: <strong> {counterService.GetServiceCheckCounter()} </strong><p>"
                        + $"<p>Endpoint calls: <strong> {counterService.GetInternalAPICallsCounter()} </strong><p>"
                        + $"<p>The old ISP adrdess was:<p>"
                        + $"<p>{oldISPAddress}<p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "ISPAdressChecker: No ISP adresses were returned");
        }
    }
}
