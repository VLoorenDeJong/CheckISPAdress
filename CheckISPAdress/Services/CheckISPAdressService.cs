using CheckISPAdress.Options;
using CheckISPAdress.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.Xml;
using static CheckISPAdress.Options.ApplicationSettingsOptions;

public class CheckISPAddressService : ICheckISPAddressService
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptions<ApplicationSettingsOptions> _applicationSettingsOptions;
    private Timer? _timer;

    private MailMessage message = new MailMessage();

    private string newISPAddress;
    private string currentISPAddress;
    private string oldISPAddress;

    public CheckISPAddressService(ILogger<CheckISPAddressService> logger, IConfiguration configuration, IOptions<ApplicationSettingsOptions> applicationSettingsOptions)
    {
        _logger = logger;
        _configuration = configuration;
        _applicationSettingsOptions = applicationSettingsOptions;

        //ToDo if mail is configured and the athor settings not send a email!
        //ToDo seperate the email settings check from API settings if mail is ok you can send a mail with tyhe config error's
        bool mailConfigChanged = DefaultSettingsHaveBeenChanged();


        if (mailConfigChanged) CreateBasicMailMessage();

    }

    public Task CheckISPAddressAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CheckISPAddress Service running.");

        double interval = (_applicationSettingsOptions?.Value?.TimeIntervalInMinutes * 60) ?? 60;
        _timer = new Timer(GetISPAddressAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(interval));

        return Task.CompletedTask;
    }

    private async void GetISPAddressAsync(object state)
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
                SendEmail(ex.Message);
            }

            if (string.Compare(newISPAddress, currentISPAddress, StringComparison.CurrentCultureIgnoreCase) > 0)
            {
                oldISPAddress = currentISPAddress;
                currentISPAddress = newISPAddress;
                
                // ToDo create a email body if the ISP addres is changed
                throw new NotImplementedException();
                //SendEmail();

            }

        }
    }
    private bool DefaultSettingsHaveBeenChanged()
    {
        bool configChanged = true;

        // ToDo check all settings and handle the configuration errrors

        if (string.Equals(_applicationSettingsOptions?.Value?.APIEndpointURL, StandardAppsettingsValues.APIEndpointURL))
        {
            Console.WriteLine("The API endpoint is not changed, change the endpoint!");
            _logger.LogInformation("The API endpoint is not changed, change the endpoint!");

        }

        return configChanged;
    }

    private void CreateBasicMailMessage()
    {
        // Set the sender, recipient, subject, and body of the message
        message.From = new MailAddress(_applicationSettingsOptions?.Value?.EmailFromAdress);
        message.To.Add(new MailAddress(_applicationSettingsOptions?.Value?.EmailToAdress));
        message.Subject = _applicationSettingsOptions?.Value?.EmailSubject;
    }

    private void SendEmail(string emailBody)
    {
        //ToDo Check confuguration if OK
        SmtpClient client = new SmtpClient();

        if (_applicationSettingsOptions?.Value is not null)
        {
            // Configure the SMTP client with your email provider's SMTP server address and credentials
            client.Host = _applicationSettingsOptions.Value.EmailHost; // Replace with your SMTP server address
            client.Port = _applicationSettingsOptions.Value.SMTPPort; // Replace with your SMTP server port number
            client.UseDefaultCredentials = _applicationSettingsOptions.Value.UseDefaultCredentials; // If your SMTP server requires authentication, set this to false
            client.Credentials = new NetworkCredential(_applicationSettingsOptions?.Value?.userName, _applicationSettingsOptions?.Value?.password); // Replace with your SMTP server username and password
            client.EnableSsl = _applicationSettingsOptions.Value.EnableSsl; // Set this to true if your SMTP server requires SSL/TLS encryption

            message.Body = emailBody;
        }

        // Send the email message
        client.Send(message);
    }
}