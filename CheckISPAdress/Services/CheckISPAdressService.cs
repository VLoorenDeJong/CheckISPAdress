using CheckISPAdress.Interfaces;
using CheckISPAdress.Options;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

public class CheckISPAddressService : ICheckISPAddressService
{
    private readonly ApplicationSettingsOptions _applicationSettingsOptions;
    private readonly IISPAdressCounterService _counterService;
    private readonly IMailService _emailService;
    private readonly ILogger _logger;

    private Dictionary<string, string> ISPAdressChecks = new();

    private string currentISPAddress;
    private string newISPAddress;
    private string oldISPAddress;
    private string ExternalISPAddress;

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

    public async Task HeartBeatCheck()
    {
        await GetISPAddressFromBackupAPIs(true);
        _emailService.SendHeartBeatEmail(_counterService, oldISPAddress, currentISPAddress, newISPAddress, ISPAdressChecks);
        ISPAdressChecks.Clear();
    }

    public async Task GetISPAddressAsync()
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
                if (_counterService.GetServiceRequestCounter() != _counterService.GetServiceCheckCounter())
                {
                    _emailService.SendCounterDifferenceEmail(_counterService);
                }
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    _counterService.AddFailedISPRequestCounter();
                    await GetISPAddressFromBackupAPIs(false);
                }
                else
                {
                    Type exceptionType = ex.GetType();

                    _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);
                    _emailService.SendISPAPIHTTPExceptionEmail(exceptionType.Name, ex.Message);
                }
                return;
            }
            catch (Exception ex)
            {
                Type exceptionType = ex.GetType();

                _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);
                _emailService.SendISPAPIExceptionEmail(exceptionType.Name, ex.Message);
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
                _emailService.SendConfigSuccessMail(newISPAddress, _counterService, _applicationSettingsOptions!.TimeIntervalInMinutes);
                await HeartBeatCheck();
            }
            else
            {
                _emailService.SendConnectionReestablishedEmail(newISPAddress, oldISPAddress, _counterService, _applicationSettingsOptions!.TimeIntervalInMinutes);
                _counterService.ResetFailedISPRequestCounter();
            }
        }
    }

    public async Task GetISPAddressFromBackupAPIs(bool heartBeatCheck)
    {
        if (ISPAdressChecks is null) ISPAdressChecks = new();
        ISPAdressChecks.Clear();

        _counterService.AddExternalServiceCheckCounter();

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

        if (ISPAdressChecks.Count > 0 && !heartBeatCheck)
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

                _emailService.SendISPAdressChangedEmail(ExternalISPAddress, oldISPAddress, _counterService, _applicationSettingsOptions!.TimeIntervalInMinutes);
            }
            else
            {
                _emailService.SendDifferendISPAdressValuesEmail(ISPAdressChecks!, oldISPAddress, _counterService, _applicationSettingsOptions!.TimeIntervalInMinutes);
            }
        }
        else if (!heartBeatCheck)
        {
            _emailService.SendNoISPAdressReturnedEmail(oldISPAddress, _counterService, _applicationSettingsOptions!.TimeIntervalInMinutes);
        }
    }
}