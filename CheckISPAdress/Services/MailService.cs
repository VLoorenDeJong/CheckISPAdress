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
                if (mailConfigured) report = ConfigHelpers.DefaultSettingsCheck(_applicationSettingsOptions, _logger);

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
        public void SendHeartBeatEmail(IISPAdressCounterService counterService, string oldISPAddress, string currentISPAddress, string newISPAddress, Dictionary<string, string> externalISPCheckResults)
        {
            string emailBody = $@"<p><strong>This was fun! </strong></p>"
                                 + $"<p>API calls:<strong> {counterService.GetServiceRequestCounter()}</strong></p>"
                                 + $"<p>API call check: <strong>{counterService.GetServiceCheckCounter()}</strong></p>"
                                 + $@"<p>Current ISP: <strong> {currentISPAddress}</strong></p>";
                                    foreach (KeyValuePair<string, string> ISPAdressCheck in externalISPCheckResults!)
                                    {
                                        string ispReport = $"<p>{ISPAdressCheck.Key} - <strong>{ISPAdressCheck.Value}</strong></p>";
                                        emailBody = $"{emailBody} {ispReport}";
                                    }
                       emailBody = $"{emailBody} <p>TimeIntervalInMinutes: <strong>{_applicationSettingsOptions?.TimeIntervalInMinutes}</strong></p>"
                                 + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                                 + $@"<p>Old ISP: <strong> {oldISPAddress}</strong></p>"
                                 + $@"<p>New ISP: <strong> {newISPAddress}</strong></p>"
                                 +$"<p>See you in {_applicationSettingsOptions?.HeatbeatEmailIntervalDays} days ;)</p>";


            SendEmail(emailBody, "ISP address checker update");
        }

        public void SendCounterDifferenceEmail(IISPAdressCounterService counterService)
        {
            string emailBody = $"<p>The ISP check counters are out of sync.</p>"
                              + $"<p>requestCounter : <strong>{counterService.GetServiceRequestCounter()}</strong></p>"
                              + $"<p>checkCounter : <strong>{counterService.GetServiceCheckCounter()}</strong></p>";

            SendEmail(emailBody, "CheckISPAddress: counter difference");
        }

        public void SendConfigSuccessMail(string newISPAddress, IISPAdressCounterService counterService, double interval)
        {
            string emailBody = $@"<p>You have succesfully configured this application.</p>"
                                  + "<p><strong>This was fun! </strong></p>"
                                  + $"<p>I wish you a splendid rest of your day!</p>"
                                  + $@"<p><strong> {newISPAddress} </strong> is your ISP adress</p>"
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
                    emailBody = $"{emailBody} " +
                                $"<p>Backup API {_applicationSettingsOptions?.BackupAPIS.IndexOf(backupAPI)} : <strong>{backupAPI}</strong></p>";
                }
            }
            // Finish the email body.
            emailBody = $"{emailBody} "
                       + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                       + $"<p>API Calls: <strong> {counterService.GetServiceRequestCounter()} </strong><p>"
                       + $"<p>Script runs: <strong> {counterService.GetServiceCheckCounter()} </strong><p>"
                       + $"<p>Failed attempts counter: <strong> {counterService.GetFailedISPRequestCounter()} </strong><p>"
                       + $"<p>Endpoint calls: <strong> {counterService.GetISPEndpointRequests()} </strong><p>"
                       + $"<p>A call is made every <strong> {interval} </strong>minutes<p>";

            SendEmail(emailBody, "ISPAdressChecker: Congratulations configuration succes!!");
        }

        public void SendConnectionReestablishedEmail(string newISPAddress, string oldISPAddress, IISPAdressCounterService counterService, double interval)
        {
            string emailBody = $@"<p>ISP adress has changed and I found my self again.</p>"
                                 + $@"<p><strong> {newISPAddress} </strong> is your new ISP adress</p>"
                                 + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                                 + "<p><strong>This is fun, hope it goes this well next time! </strong></p>"
                                 + $"<p>I wish you a splendid rest of your day!</p>"
                                 + $"<p>Your API</p>"
                                 + $"<p><strong>Here are some statistics:</strong></p>"
                                 + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                                 + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                                 + $"<p>Failed attempts counter: <strong> {counterService.GetFailedISPRequestCounter()} </strong>(This counter is reset after this E-mail is send)<p>"
                                 + $"<p>API Calls: <strong> {counterService.GetServiceRequestCounter()} </strong><p>"
                                 + $"<p>Script runs: <strong> {counterService.GetServiceCheckCounter()} </strong><p>"
                                 + $"<p>Endpoint calls: <strong> {counterService.GetISPEndpointRequests()} </strong><p>"
                                 + $"<p>The old ISP adrdess was: {oldISPAddress}<p>"
                                 + $"<p>API endpoint URL: <strong><a href{_applicationSettingsOptions?.APIEndpointURL}</strong></p>";

            SendEmail(emailBody, "ISPAdressChecker: ISP adress changed but I found my seld");
        }

        public void SenISPAPIHTTPExceptionEmail(string exceptionType, string exceptionMessage)
        {
            string message = $"<p>API Did not respond:</p>"
                           + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                           +  "<p>exceptionType:</p>"
                           + $"<p><strong>{exceptionType}</strong></p>"
                           +  "<p>message:</p>"
                           + $"<p><strong>{exceptionMessage}<strong></p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "CheckISPAddress: API endpoint HTTP exception");
        }

        public void SendISPAPIEceptionEmail(string exceptionType, string exceptionMessage)
        {
            _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, exceptionMessage);

            string message = $"<p>Exception fetching ISP address from API:</p>"
                           + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                           +  "<p>exceptionType:"
                           + $"<p><strong>{exceptionType}</strong></p>"
                           +  "<p>message:"
                           + $"<p><strong>{exceptionMessage}<strong></p>";

            string emailBody = CreateEmail(message);



            SendEmail(emailBody, "CheckISPAddress: API Call error");
        }

        public void SendExternalAPIHTTPExceptionEmail(string APIUrl, string exceptionType, string exceptionMessage)
        {

            string message =  $"<p>API Did not respond:</p>"
                            + $"<p><strong>{APIUrl}</strong></p>"
                            +  "<p>exceptionType:</p>"
                            + $"<p><strong>{exceptionType}</strong></p>"
                            +  "<p>message:</p>"
                            + $"<p><strong>{exceptionMessage}<strong></p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "CheckISPAddress: Backup API HTTP exception");
        }

        public void SendExternalAPIExceptionEmail(string APIUrl, string exceptionType, string exceptionMessage)
        {
            string message = $"<p>Exception fetching ISP address from API:</p>"
                           + $"<p><strong>{APIUrl}</strong></p>"
                           +  "<p>exceptionType:"
                           + $"<p><strong>{exceptionType}</strong></p>"
                           +  "<p>message:"
                           + $"<p><strong>{exceptionMessage}<strong></p>";

            string emailBody = CreateEmail(message);

            SendEmail(emailBody, "CheckISPAddress: API Call error");
        }

        public void SendISPAdressChangedEmail(string externalISPAddress, string oldISPAddress, IISPAdressCounterService counterService, double interval)
        {
            // hostingProviderText is the link to the hostprovider, id specified is shows the name
            string hostingProviderText = string.Equals(_applicationSettingsOptions?.DNSRecordHostProviderURL, StandardAppsettingsValues.DNSRecordHostProviderName, StringComparison.CurrentCultureIgnoreCase) ? _applicationSettingsOptions?.DNSRecordHostProviderURL! : _applicationSettingsOptions?.DNSRecordHostProviderName!;

            string emailBody = $@"<p><strong> {externalISPAddress} </strong> is your new ISP adress</p>"
                              + $"<p>Go to <a href = '{_applicationSettingsOptions?.DNSRecordHostProviderURL}'> <strong>{hostingProviderText}</strong> </a> to update the DNS record.</p>"
                              + $"<p>I wish you a splendid rest of your day!</p>"
                              + $"<p>Your API</p>"
                              + $"<p><strong>Here are some statistics:</strong></p>"
                              + $"<p>API endpoint URL:<a href = '{_applicationSettingsOptions?.APIEndpointURL}'> <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></a></p>"
                              + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                              + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                              + $"<p>Failed attempts counter: <strong> {counterService.GetFailedISPRequestCounter()} </strong><p>"
                              + $"<p>API Calls: <strong> {counterService.GetServiceRequestCounter()} </strong><p>"
                              + $"<p>Script runs: <strong> {counterService.GetServiceCheckCounter()} </strong><p>"
                              + $"<p>Endpoint calls: <strong> {counterService.GetISPEndpointRequests()} </strong><p>"
                              + $"<p>The old ISP adrdess was:<p>"
                              + $"<p>{oldISPAddress}<p>"
                              ;

            SendEmail(emailBody, _applicationSettingsOptions?.EmailSubject!);
        }

        public void SendDifferendISPAdressValuesEmail(Dictionary<string, string> externalISPAdressChecks, string oldISPAddress, IISPAdressCounterService counterService, double interval)
        {
            string emailBody = $@"<p><strong> Multiple </strong> ISP adresses returned</p>";

            foreach (KeyValuePair<string, string> ISPAdressCheck in externalISPAdressChecks!)
            {
                string ispReport = $"<p>{ISPAdressCheck.Key} - <strong>{ISPAdressCheck.Value}</strong></p>";
                emailBody = $"{emailBody} {ispReport}";
            }

            emailBody = $"{emailBody}"
                        + "<p><strong>Best of luck solving this one!</strong></p>"
                        + $"<p>I wish you a splendid rest of your day!</p>"
                        + $"<p>Your API</p>" + $"<p><strong>Here are some statistics:</strong></p>"
                        + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                        + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                        + $"<p>Failed attempts counter: <strong> {counterService.GetFailedISPRequestCounter} </strong><p>"
                        + $"<p>API Calls: <strong> {counterService.GetServiceRequestCounter()} </strong><p>"
                        + $"<p>Script runs: <strong> {counterService.GetServiceCheckCounter()} </strong><p>"
                        + $"<p>Endpoint calls: <strong> {counterService.GetISPEndpointRequests()} </strong><p>"
                        + $"<p>The old ISP adrdess was:<p>"
                        + $"<p>{oldISPAddress}<p>";

            SendEmail(emailBody, "ISPAdressChecker: multiple ISP adresses were returned");
        }

        public void SendNoISPAdressReturnedEmail(string oldISPAddress, IISPAdressCounterService counterService, double interval)
        {

            string emailBody = $@"<p>No adresses were returned and no exceptions?!?!</p>"
                        + "<p><strong>Best of luck solving this one!</strong></p>"
                        + $"<p>I wish you a splendid rest of your day!</p>"
                        + $"<p>Your API</p>" + $"<p><strong>Here are some statistics:</strong></p>"
                        + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                        + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions.DateTimeFormat)} </strong><p>"
                        + $"<p>Failed attempts counter: <strong> {counterService.GetFailedISPRequestCounter()} </strong><p>"
                        + $"<p>API Calls: <strong> {counterService.GetServiceRequestCounter()} </strong><p>"
                        + $"<p>Script runs: <strong> {counterService.GetServiceCheckCounter()} </strong><p>"
                        + $"<p>Endpoint calls: <strong> {counterService.GetISPEndpointRequests()} </strong><p>"
                        + $"<p>The old ISP adrdess was:<p>"
                        + $"<p>{oldISPAddress}<p>";

            SendEmail(emailBody, "ISPAdressChecker: No ISP adresses were returned");
        }
    }
}
