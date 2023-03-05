using CheckISPAdress.Helpers;
using CheckISPAdress.Interfaces;
using CheckISPAdress.Models;
using CheckISPAdress.Options;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static CheckISPAdress.Options.ApplicationSettingsOptions;

public class CheckISPAddressService : ICheckISPAddressService
{
    private readonly ApplicationSettingsOptions _applicationSettingsOptions;
    private readonly IISPAdressCounterService _counterService;
    private readonly IMailService _emailService;
    private readonly ILogger _logger;

    private Timer? checkCounterTimer;
    private Timer? emailTimer;

    private Dictionary<string, string> ISPAdressChecks = new();

    private string currentISPAddress;
    private string newISPAddress;
    private string oldISPAddress;

    private double interval;

    public CheckISPAddressService(ILogger<CheckISPAddressService> logger, IOptions<ApplicationSettingsOptions> applicationSettingsOptions, IMailService emailService, IISPAdressCounterService counterService)
    {
        _logger = logger;
        _applicationSettingsOptions = applicationSettingsOptions?.Value!;
        _emailService = emailService;
        _counterService = counterService;

        newISPAddress = string.Empty;
        currentISPAddress = string.Empty;
        oldISPAddress = string.Empty;
    }

    public Task CheckISPAddressAsync(CancellationToken cancellationToken)
    {
        ConfigErrorReportModel report = ConfigHelpers.DefaultSettingsHaveBeenChanged(_applicationSettingsOptions, _logger);

        if (report.ChecksPassed)
        {
            _logger.LogInformation("CheckISPAddress Service running.");

            interval = (_applicationSettingsOptions.TimeIntervalInMinutes == 0) ? 60 : _applicationSettingsOptions.TimeIntervalInMinutes;

            emailTimer = new Timer(async (state) => await GetISPAddressAsync(state!), null, TimeSpan.Zero, TimeSpan.FromMinutes(interval));
            checkCounterTimer = new Timer(state => { _counterService.ServiceCheckCounter++; }, null, TimeSpan.FromMinutes(interval), TimeSpan.FromMinutes(interval));

            return Task.CompletedTask;
        }

        return Task.FromException(new TaskCanceledException());
    }

    private async Task GetISPAddressAsync(object state)
    {
        using (var client = new HttpClient())
        {
            try
            {
                _counterService.ServiceRequestCounter++;

                HttpResponseMessage response = await client.GetAsync(_applicationSettingsOptions?.APIEndpointURL);
                response.EnsureSuccessStatusCode();

                newISPAddress = string.Empty;
                newISPAddress = await response?.Content?.ReadAsStringAsync()!;

                // Checking if the counters are still in sync 
                if (_counterService.ServiceRequestCounter != _counterService.ServiceCheckCounter)
                {
                    string emailBody = $"<p>The ISP check counters are out of sync.</p>"
                                      +$"<p>requestCounter : <strong>{_counterService.ServiceRequestCounter}</strong></p>"
                                      +$"<p>checkCounter : <strong>{_counterService.ServiceCheckCounter}</strong></p>";

                    _emailService.SendEmail(emailBody, "CheckISPAddress: counter difference");
                }
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                   _counterService.FailedISPRequestCounter++;
                    await GetISPAddressFromBackupAPIs();
                }
                else
                {
                    Type exceptionType = ex.GetType();

                    _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);

                    string message = $"<p>API Did not respond:</p>"
                                   + $"<p><strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></p>"
                                   + "<p>exceptionType:</p>"
                                   + $"<p><strong>{exceptionType}</strong></p>"
                                   + "<p>message:</p>"
                                   + $"<p><strong>{ex.Message}<strong></p>";

                    string emailBody = _emailService.CreateEmail(message);

                    _emailService.SendEmail(emailBody, "CheckISPAddress: API endpoint HTTP exception");
                }
            }
            catch (Exception ex)
            {

                Type exceptionType = ex.GetType();

                _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);
                string message = $"<p>Exception fetching ISP address from API:</p>"
                               + $"<p><strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></p>"
                               + "<p>exceptionType:"
                               + $"<p><strong>{exceptionType}</strong></p>"
                               + "<p>message:"
                               + $"<p><strong>{ex.Message}<strong></p>";

                string emailBody = _emailService.CreateEmail(message);



                _emailService.SendEmail(emailBody, "CheckISPAddress: API Call error");
            }

            if (!string.Equals(newISPAddress, currentISPAddress, StringComparison.CurrentCultureIgnoreCase))
            {
                // Copy the old ISP adress to that variable
                oldISPAddress = currentISPAddress;
                // Make the new ISP address the current address
                currentISPAddress = newISPAddress;

                // hostingProviderText is the link to the hostprovider, id specified is shows the name
                string hostingProviderText = string.Equals(_applicationSettingsOptions?.DNSRecordHostProviderURL, StandardAppsettingsValues.DNSRecordHostProviderName, StringComparison.CurrentCultureIgnoreCase) ? _applicationSettingsOptions?.DNSRecordHostProviderURL! : _applicationSettingsOptions?.DNSRecordHostProviderName!;

                if (_counterService.ServiceRequestCounter == 1)
                {
                    string emailBody = $@"<p>You have succesfully configured this application.</p>"
                                      +  "<p><strong>This was fun! </strong></p>"
                                      + $"<p>I wish you a splendid rest of your day!</p>"
                                      +$@"<p><strong> {newISPAddress} </strong> is your ISP adress</p>"
                                      +$@"<br />"
                                      +$@"<br />"
                                      + $"<p><strong>The folowing things were configured:</strong></p>"
                                      + $"<p>API endpoint URL: <strong>{_applicationSettingsOptions?.APIEndpointURL}</strong></p>"
                                      + $"<p>TimeIntervalInMinutes: <strong>{_applicationSettingsOptions?.TimeIntervalInMinutes}</strong></p>"
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
                    if(_applicationSettingsOptions?.BackupAPIS is not null)
                    {
                        foreach (string? backupAPI in _applicationSettingsOptions?.BackupAPIS!)
                        {
                            emailBody = $"{emailBody} " +
                                        $"<p>Backup API {_applicationSettingsOptions?.BackupAPIS.IndexOf(backupAPI)} : <strong>{backupAPI}</strong></p>";
                        }
                    }
                    // Finish the email body.
                    emailBody = $"{emailBody} " 
                               +$"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                               +$"<p>API Calls: <strong> {_counterService.ServiceRequestCounter} </strong><p>"
                               +$"<p>Script runs: <strong> {_counterService.ServiceCheckCounter} </strong><p>"
                               +$"<p>Failed attempts counter: <strong> {_counterService.FailedISPRequestCounter} </strong><p>"
                               + $"<p>Endpoint calls: <strong> {_counterService.ISPEndpointRequests} </strong><p>"
                               + $"<p>A call is made every <strong> {interval} </strong>minutes<p>";

                    _emailService.SendEmail(emailBody, "ISPAdressChecker: Congratulations configuration succes!!");

                }

                else
                {
                    string emailBody = $@"<p>ISP adress has changed and I found my self again.</p>"
                                      +$@"<p><strong> {newISPAddress} </strong> is your new ISP adress</p>"
                                      +  "<p><strong>This is fun, hope it goes this well next time! </strong></p>"
                                      + $"<p>I wish you a splendid rest of your day!</p>"
                                      + $"<p>Your API</p>" 
                                      + $"<p><strong>Here are some statistics:</strong></p>"
                                      + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                                      + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                                      + $"<p>Failed attempts counter: <strong> {_counterService.FailedISPRequestCounter} </strong>(This counter is reset after this E-mail is send)<p>"
                                      + $"<p>API Calls: <strong> {_counterService.ServiceRequestCounter} </strong><p>"
                                      + $"<p>Script runs: <strong> {_counterService.ServiceCheckCounter} </strong><p>"
                                      + $"<p>Endpoint calls: <strong> {_counterService.ISPEndpointRequests} </strong><p>"
                                      + $"<p>The old ISP adrdess was:<p>"
                                      + $"<p>{oldISPAddress}<p>";

                    _emailService.SendEmail(emailBody, "ISPAdressChecker: ISP adress changed but I found my seld");
                    _counterService.FailedISPRequestCounter = 0;
                }
            }
        }
    }

    private async Task GetISPAddressFromBackupAPIs()
    {
        if (ISPAdressChecks is null) ISPAdressChecks = new();
        ISPAdressChecks.Clear();

        foreach (string? APIUrl in _applicationSettingsOptions?.BackupAPIS!)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // Testing code
                    //throw new HttpRequestException();
                    //throw new Exception();
                    //requestCounter++;
                    //int ISPAddress = requestCounter + 1;
                    //ISPAdressChecks.Add(APIUrl!, ISPAddress.ToString());
                    //ISPAdressChecks.Clear();


                    HttpResponseMessage response = await client.GetAsync(APIUrl);
                    response.EnsureSuccessStatusCode();

                    string ISPAddress = await response.Content.ReadAsStringAsync();

                    Match match = Regex.Match(ISPAddress, @"\b(?:\d{1,3}\.){3}\d{1,3}\b");
                    if (match.Success)
                    {
                        ISPAddress = match.Value; // Output: ISP adress
                    }

                    ISPAdressChecks.Add(APIUrl!, ISPAddress);
                }
                catch (HttpRequestException ex)
                {
                    Type exceptionType = ex.GetType();

                    _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);

                    string message = $"<p>API Did not respond:</p>"
                                    +$"<p><strong>{APIUrl}</strong></p>"
                                    + "<p>exceptionType:</p>"
                                    +$"<p><strong>{exceptionType}</strong></p>"
                                    + "<p>message:</p>"
                                    +$"<p><strong>{ex.Message}<strong></p>";

                    string emailBody = _emailService.CreateEmail(message);

                    _emailService.SendEmail(emailBody, "CheckISPAddress: Backup API HTTP exception");
                }
                catch (Exception ex)
                {

                    Type exceptionType = ex.GetType();

                    _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);

                    string message = $"<p>Exception fetching ISP address from API:</p>"
                                    +$"<p><strong>{APIUrl}</strong></p>"
                                    + "<p>exceptionType:"
                                    +$"<p><strong>{exceptionType}</strong></p>"
                                    + "<p>message:"
                                    +$"<p><strong>{ex.Message}<strong></p>";

                    string emailBody = _emailService.CreateEmail(message);

                    _emailService.SendEmail(emailBody, "CheckISPAddress: API Call error");
                }
            }
        }

        if (ISPAdressChecks.Count > 0)
        {
            // Get the uniwue ISP adresses from the dictionary
            List<string>? uniqueAdresses = ISPAdressChecks?.Values?.Distinct()?.ToList()!;

            // hostingProviderText is the link to the hostprovider, id specified is shows the name
            string hostingProviderText = string.Equals(_applicationSettingsOptions?.DNSRecordHostProviderURL, StandardAppsettingsValues.DNSRecordHostProviderName, StringComparison.CurrentCultureIgnoreCase) ? _applicationSettingsOptions?.DNSRecordHostProviderURL! : _applicationSettingsOptions?.DNSRecordHostProviderName!;

            if (uniqueAdresses.Count == 1)
            {
                // Update new ISP adress
                newISPAddress = uniqueAdresses[0]!;
                // Copy the old ISP adress to that variable
                oldISPAddress = currentISPAddress;
                // Make the new ISP address the current address
                currentISPAddress = newISPAddress;

                string emailBody = $@"<p><strong> {newISPAddress} </strong> is your new ISP adress</p>"
                                  + $"<p>Go to <a href = '{_applicationSettingsOptions?.DNSRecordHostProviderURL}'> <strong>{hostingProviderText}</strong> </a> to update the DNS record.</p>"
                                  + $"<p>I wish you a splendid rest of your day!</p>"
                                  + $"<p>Your API</p>"
                                  + $"<p><strong>Here are some statistics:</strong></p>"
                                  + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                                  + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                                  + $"<p>Failed attempts counter: <strong> {_counterService.FailedISPRequestCounter} </strong><p>"
                                  + $"<p>API Calls: <strong> {_counterService.ServiceRequestCounter} </strong><p>"
                                  + $"<p>Script runs: <strong> {_counterService.ServiceCheckCounter} </strong><p>"
                                  + $"<p>Endpoint calls: <strong> {_counterService.ISPEndpointRequests} </strong><p>"
                                  + $"<p>The old ISP adrdess was:<p>"
                                  + $"<p>{oldISPAddress}<p>"
                                  ;

                _emailService.SendEmail(emailBody, _applicationSettingsOptions?.EmailSubject!);
            }
            else
            {
                string emailBody = $@"<p><strong> Multiple </strong> ISP adresses returned</p>";

                foreach (KeyValuePair<string, string> ISPAdressCheck in ISPAdressChecks!)
                {
                    string ispReport = $"<p>URL:{ISPAdressCheck.Key} Adress: <strong>{ISPAdressCheck.Value}</strong></p>";
                    emailBody = $"{emailBody} {ispReport}";
                }

                emailBody = $"{emailBody}"
                            + "<p><strong>Best of luck solving this one!</strong></p>"
                            + $"<p>I wish you a splendid rest of your day!</p>"
                            + $"<p>Your API</p>" + $"<p><strong>Here are some statistics:</strong></p>"
                            + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                            + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions?.DateTimeFormat)} </strong><p>"
                            + $"<p>Failed attempts counter: <strong> {_counterService.FailedISPRequestCounter} </strong><p>"
                            + $"<p>API Calls: <strong> {_counterService.ServiceRequestCounter} </strong><p>"
                            + $"<p>Script runs: <strong> {_counterService.ServiceCheckCounter} </strong><p>"
                            + $"<p>Endpoint calls: <strong> {_counterService.ISPEndpointRequests} </strong><p>"
                            + $"<p>The old ISP adrdess was:<p>"
                            + $"<p>{oldISPAddress}<p>";

                _emailService.SendEmail(emailBody, "ISPAdressChecker: multiple ISP adresses were returned");
            }
        }
        else
        {
            string emailBody = $@"<p>No adresses were returned and no exceptions?!?!</p>"
                        + "<p><strong>Best of luck solving this one!</strong></p>"
                        + $"<p>I wish you a splendid rest of your day!</p>"
                        + $"<p>Your API</p>" + $"<p><strong>Here are some statistics:</strong></p>"
                        + $"<p>A call is made every <strong> {interval} </strong>minutes<p>"
                        + $"<p>The time of this check: <strong> {DateTime.Now.ToString(_applicationSettingsOptions.DateTimeFormat)} </strong><p>"
                        + $"<p>Failed attempts counter: <strong> {_counterService.FailedISPRequestCounter} </strong><p>"
                        + $"<p>API Calls: <strong> {_counterService.ServiceRequestCounter} </strong><p>"
                        + $"<p>Script runs: <strong> {_counterService.ServiceCheckCounter} </strong><p>"
                        + $"<p>Endpoint calls: <strong> {_counterService.ISPEndpointRequests} </strong><p>"
                        + $"<p>The old ISP adrdess was:<p>"
                        + $"<p>{oldISPAddress}<p>";

            _emailService.SendEmail(emailBody, "ISPAdressChecker: No ISP adresses were returned");
        }
    }
}