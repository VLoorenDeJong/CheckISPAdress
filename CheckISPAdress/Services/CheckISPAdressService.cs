using CheckISPAdress.Helpers;
using CheckISPAdress.Interfaces;
using CheckISPAdress.Models;
using CheckISPAdress.Options;
using Microsoft.Extensions.Options;
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
    private string ExternalISPAddress;

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
        ExternalISPAddress = string.Empty;
    }

    public Task CheckISPAddressAsync(CancellationToken cancellationToken)
    {
        ConfigErrorReportModel report = ConfigHelpers.DefaultSettingsHaveBeenChanged(_applicationSettingsOptions, _logger);

        if (report.ChecksPassed)
        {
            _logger.LogInformation("CheckISPAddress Service running.");

            interval = (_applicationSettingsOptions.TimeIntervalInMinutes == 0) ? 60 : _applicationSettingsOptions.TimeIntervalInMinutes;

            emailTimer = new Timer(async (state) => await GetISPAddressAsync(state!), null, TimeSpan.Zero, TimeSpan.FromMinutes(interval));
            checkCounterTimer = new Timer(state => { _counterService.AddServiceCheckCounter(); }, null, TimeSpan.FromMinutes(interval), TimeSpan.FromMinutes(interval));

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
                _counterService.AddServiceRequestCounter();

                HttpResponseMessage response = await client.GetAsync(_applicationSettingsOptions?.APIEndpointURL);
                response.EnsureSuccessStatusCode();

                newISPAddress = string.Empty;
                newISPAddress = await response?.Content?.ReadAsStringAsync()!;


                // Checking if the counters are still in sync 
                if (_counterService.GetServiceRequestCounter != _counterService.GetServiceCheckCounter)
                {
                    _emailService.SendCounterDifferenceEmail(_counterService);
                }
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                   _counterService.AddFailedISPRequestCounter();
                    await GetISPAddressFromBackupAPIs();
                }
                else
                {
                    Type exceptionType = ex.GetType();

                    _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);
                    _emailService.SenISPAPIHTTPExceptionEmail(exceptionType.Name, ex.Message);
                }
                return;
            }
            catch (Exception ex)
            {
                Type exceptionType = ex.GetType();

                _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);
                _emailService.SendISPAPIEceptionEmail(exceptionType.Name, ex.Message);
                return;
            }
        }


        if (!string.Equals(newISPAddress, currentISPAddress, StringComparison.CurrentCultureIgnoreCase))
        {
            // Copy the old ISP adress to that variable
            oldISPAddress = currentISPAddress;
            // Make the new ISP address the current address
            currentISPAddress = newISPAddress;

            if (_counterService.GetServiceRequestCounter() == 1 && _counterService.GetFailedISPRequestCounter() == 0)
            {
                _emailService.SendConfigSuccessMail(newISPAddress, _counterService, interval);
            }
            else
            {
                _emailService.SendConnectionReestablishedEmail(newISPAddress, oldISPAddress, _counterService, interval);
                _counterService.ResetFailedISPRequestCounter();
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
                    _emailService.SendExternalAPIHTTPExceptionEmail(APIUrl!, exceptionType.Name, ex.Message);

                }
                catch (Exception ex)
                {

                    Type exceptionType = ex.GetType();

                    _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);

                    _emailService.SendExternalAPIExceptionEmail(APIUrl!, exceptionType.Name, ex.Message);
                }
            }
        }

        if (ISPAdressChecks.Count > 0)
        {
            // Get the uniwue ISP adresses from the dictionary
            List<string>? uniqueAdresses = ISPAdressChecks?.Values?.Distinct()?.ToList()!;
            

            if (uniqueAdresses.Count == 1)
            {
                // Update new ISP adress
                ExternalISPAddress = uniqueAdresses[0]!;
                // Copy the old ISP adress to that variable
                oldISPAddress = currentISPAddress;
                currentISPAddress = string.Empty;

                _emailService.SendISPAdressChangedEmail(ExternalISPAddress, oldISPAddress, _counterService, interval);
            }
            else
            {
                _emailService.SendDifferendISPAdressValuesEmail(ISPAdressChecks!, oldISPAddress, _counterService, interval);
            }
        }
        else
        {
            _emailService.SendNoISPAdressReturnedEmail(oldISPAddress, _counterService, interval);
        }
    }
}