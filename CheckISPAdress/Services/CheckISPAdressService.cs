using CheckISPAdress.Interfaces;
using CheckISPAdress.Options;
using CheckISPAdress.Services;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

public class CheckISPAddressService : ICheckISPAddressService
{
    private readonly ApplicationSettingsOptions _applicationSettingsOptions;
    private readonly IISPAdressCounterService _counterService;
    private readonly IISPAddressService _ISPAdressService;
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;

    private Dictionary<string, string> ISPAdressChecks = new();

    public CheckISPAddressService(ILogger<CheckISPAddressService> logger, IOptions<ApplicationSettingsOptions> applicationSettingsOptions, IEmailService emailService, IISPAdressCounterService counterService, IISPAddressService ISPAdressService)
    {
        _logger = logger;
        _applicationSettingsOptions = applicationSettingsOptions?.Value!;
        _emailService = emailService;
        _counterService = counterService;
        _ISPAdressService = ISPAdressService;
    }

    public async Task HeartBeatCheck()
    {
        await GetISPAddressFromBackupAPIs(true);
        _emailService.SendHeartBeatEmail(_counterService, _ISPAdressService.GetOldISPAddress(), _ISPAdressService.GetCurrentISPAddress(), _ISPAdressService.GetNewISPAddress(), ISPAdressChecks);
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

                _ISPAdressService.ClearNewISPAddress();
                _ISPAdressService.SetNewISPAddress(await response?.Content?.ReadAsStringAsync()!);

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

        if (!string.Equals(_ISPAdressService.GetNewISPAddress(), _ISPAdressService.GetCurrentISPAddress(), StringComparison.CurrentCultureIgnoreCase))
        {
            // Copy the old ISP adress to that variable
            _ISPAdressService.SetOldISPAddress(_ISPAdressService.GetCurrentISPAddress());
            // Make the new ISP address the current address
            _ISPAdressService.SetCurrentISPAddress(_ISPAdressService.GetNewISPAddress());

            if (_counterService.GetServiceRequestCounter() == 1 && _counterService.GetFailedISPRequestCounter() == 0)
            {
                await HeartBeatCheck();
            }
            else
            {
                _emailService.SendConnectionReestablishedEmail(_ISPAdressService.GetNewISPAddress(), _ISPAdressService.GetOldISPAddress(), _counterService, _applicationSettingsOptions!.TimeIntervalInMinutes);
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
                _ISPAdressService.SetExternalISPAddress(uniqueAdresses[0]!);
                // Copy the old ISP adress to that variable
                _ISPAdressService.SetOldISPAddress(_ISPAdressService.GetCurrentISPAddress());
                _ISPAdressService.ClearCurrentISPAddress();

                _emailService.SendISPAdressChangedEmail(_ISPAdressService.GetExternalISPAddress(), _ISPAdressService.GetOldISPAddress(), _counterService, _applicationSettingsOptions!.TimeIntervalInMinutes);
            }
            else
            {
                _emailService.SendDifferendISPAdressValuesEmail(ISPAdressChecks!, _ISPAdressService.GetOldISPAddress(), _counterService, _applicationSettingsOptions!.TimeIntervalInMinutes);
            }
        }
        else if (!heartBeatCheck)
        {
            _emailService.SendNoISPAdressReturnedEmail(_ISPAdressService.GetOldISPAddress(), _counterService, _applicationSettingsOptions!.TimeIntervalInMinutes);
        }
    }
}