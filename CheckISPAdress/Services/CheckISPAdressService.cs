using CheckISPAdress.Helpers;
using CheckISPAdress.Models;
using CheckISPAdress.Options;
using CheckISPAdress.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.Xml;
using static CheckISPAdress.Options.ApplicationSettingsOptions;

public class CheckISPAddressService : ICheckISPAddressService
{
    private readonly ILogger _logger;
    private readonly ApplicationSettingsOptions _applicationSettingsOptions;
    private readonly IMailService _emailService;

    private Timer? emailTimer;
    private Timer? checkCounterTimer;

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

        interval = (_applicationSettingsOptions?.TimeIntervalInMinutes * 60) ?? 60;

        emailTimer = new Timer(async (state) => await GetISPAddressAsync(state!), null, TimeSpan.Zero, TimeSpan.FromSeconds(interval));       
        checkCounterTimer = new Timer(state => {checkCounter++;}, null, TimeSpan.FromSeconds(interval), TimeSpan.FromSeconds(interval));
      

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
                newISPAddress = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("API Call error. Message:{message}", ex.Message);
                _emailService.SendEmail(ex.Message);
            }

            if (!string.Equals(newISPAddress, currentISPAddress, StringComparison.CurrentCultureIgnoreCase))
            {
                oldISPAddress = currentISPAddress;
                currentISPAddress = newISPAddress;


                string emailBody = _emailService.ISPAddressChangedEmail(newISPAddress, oldISPAddress, _applicationSettingsOptions!.DNSRecordHostingProviderURL, interval, requestCounter, checkCounter);
                _emailService.SendEmail(emailBody);

            }

        }
    }
}