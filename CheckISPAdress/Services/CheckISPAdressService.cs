using CheckISPAdress.Helpers;
using CheckISPAdress.Interfaces;
using CheckISPAdress.Models;
using CheckISPAdress.Options;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

public class CheckISPAddressService : ICheckISPAddressService
{
    private readonly ILogger _logger;
    private readonly ApplicationSettingsOptions _applicationSettingsOptions;
    private readonly IMailService _emailService;

    private Timer? emailTimer;
    private Timer? checkCounterTimer;

    private List<string> ISPAdresses = new List<string>();

    private string newISPAddress;
    private string currentISPAddress;
    private string oldISPAddress;

    private int requestCounter = 0;
    private int checkCounter = 1;

    private double interval;

    public CheckISPAddressService(ILogger<CheckISPAddressService> logger, IOptions<ApplicationSettingsOptions> applicationSettingsOptions, IMailService emailService)
    {
        _logger = logger;
        _applicationSettingsOptions = applicationSettingsOptions?.Value!;
        _emailService = emailService;

        newISPAddress = string.Empty;
        currentISPAddress = string.Empty;
        oldISPAddress = string.Empty;
    }

    public Task CheckISPAddressAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CheckISPAddress Service running.");

        interval = (_applicationSettingsOptions.TimeIntervalInMinutes == 0) ? 60 : _applicationSettingsOptions.TimeIntervalInMinutes;

        emailTimer = new Timer(async (state) => await GetISPAddressAsync(state!), null, TimeSpan.Zero, TimeSpan.FromMinutes(interval));
        checkCounterTimer = new Timer(state => { checkCounter++; }, null, TimeSpan.FromMinutes(interval), TimeSpan.FromMinutes(interval));

        return Task.CompletedTask;
    }

    private async Task GetISPAddressAsync(object state)
    {
        using (var client = new HttpClient())
        {
            try
            {
                requestCounter++;

                HttpResponseMessage response = await client.GetAsync(_applicationSettingsOptions?.APIEndpointURL);
                response.EnsureSuccessStatusCode();

                newISPAddress = string.Empty;
                newISPAddress = await response?.Content?.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    await GetISPAddressFromBackupAPIs();
                }
                else
                {
                    Type exceptionType = ex.GetType();

                    _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);
                    string emailBody = $"exceptionType:{exceptionType}, message: {ex.Message}";

                    _emailService.SendEmail(emailBody);
                }
            }
            catch (Exception ex)
            {

                Type exceptionType = ex.GetType();

                _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);
                string emailBody = $"exceptionType:{exceptionType}, message: {ex.Message}";

                _emailService.SendEmail(emailBody);
            }

            if (!string.Equals(newISPAddress, currentISPAddress, StringComparison.CurrentCultureIgnoreCase))
            {
                oldISPAddress = currentISPAddress;
                currentISPAddress = newISPAddress;


                string emailBody = _emailService.ISPAddressChangedEmail(newISPAddress, interval, requestCounter, checkCounter);
                _emailService.SendEmail(emailBody);

            }
        }
    }

    private async Task GetISPAddressFromBackupAPIs()
    {
        if (ISPAdresses is null) ISPAdresses = new();
        ISPAdresses.Clear();

        List<string> requestedUrls = new();
        int emailcount = 0;

        foreach (string? APIUrl in _applicationSettingsOptions?.BackupAPIS!)
        {
            using (var client = new HttpClient())
            {
                try
                {

                    ISPAdresses.Add("192.168.2.13");
                    //HttpResponseMessage response = await client.GetAsync(APIUrl);
                    //response.EnsureSuccessStatusCode();

                    //string ISPAddress = await response.Content.ReadAsStringAsync();

                    //Match match = Regex.Match(ISPAddress, @"\b(?:\d{1,3}\.){3}\d{1,3}\b");
                    //if (match.Success)
                    //{
                    //    ISPAddress = match.Value; // Output: ISP adress
                    //}

                    //ISPAdresses.Add(ISPAddress);
                }
                catch (HttpRequestException ex)
                {
                    Type exceptionType = ex.GetType();

                    _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);
                    string emailBody = $"API Did not respond: <br /> {APIUrl} <br /> <br />exceptionType:{exceptionType} <br /> message: {ex.Message}";

                    _emailService.SendEmail(emailBody);
                    emailcount++;


                }
                catch (Exception ex)
                {

                    Type exceptionType = ex.GetType();

                    _logger.LogError("API Call error. Exceptiontype: {type} Message:{message}", exceptionType, ex.Message);
                    string emailBody = $"API Did not respond: <br /> {APIUrl} <br /> <br />exceptionType:{exceptionType} <br /> message: {ex.Message}";

                    _emailService.SendEmail(emailBody);
                }
            }
        }
    }
}