using CheckISPAdress.Options;
using CheckISPAdress.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

public class CheckISPAddressService : ICheckISPAddressService
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptions<ApplicationSettingsOptions> _applicationSettingsOptions;
    private Timer? _timer;

    private string newISPAddress;
    private string currentISPAddress;
    private string oldISPAddress;

    public CheckISPAddressService(ILogger<CheckISPAddressService> logger, IConfiguration configuration, IOptions<ApplicationSettingsOptions> applicationSettingsOptions)
    {
        _logger = logger;
        _configuration = configuration;
        _applicationSettingsOptions = applicationSettingsOptions;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CheckISPAddress Service running.");

        double interval = (_applicationSettingsOptions?.Value?.TimeIntervalInMinutes*60) ?? 60;
        _timer = new Timer(CheckISPAddress, null, TimeSpan.Zero, TimeSpan.FromSeconds(interval));

        return Task.CompletedTask;
    }

    private async void CheckISPAddress(object state)
    {
        using (var client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(_applicationSettingsOptions?.Value?.APIEndpointURL);
                response.EnsureSuccessStatusCode();

                newISPAddress = string.Empty;
                newISPAddress = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) 
            {
                _logger.LogError("API Call error. Message:{message}", ex.Message);
            }

            if(string.Compare(newISPAddress, currentISPAddress, StringComparison.CurrentCultureIgnoreCase) > 0)
            {
                oldISPAddress = currentISPAddress;
                currentISPAddress = newISPAddress;  

            }
            
        }
    }
}