using CheckISPAdress.Helpers;
using CheckISPAdress.Models;
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

        if (_applicationSettingsOptions is not null)
        {
            bool mailConfigured = true;
            ConfigErrorReportModel report = new ConfigErrorReportModel {ChecksPassed = true };

            mailConfigured = ConfigHelpers.DefaultMailSettingsChanged(_applicationSettingsOptions, _logger);
            if (mailConfigured) CreateBasicMailMessage();
            if (mailConfigured) report = ConfigHelpers.DefaultSettingsHaveBeenChanged(_applicationSettingsOptions, _logger);
           
            if (!(report.ChecksPassed))
            {
                string completeMessage = "";
                foreach (string message in report.ErrorMessages)
                {
                    completeMessage = $"{completeMessage}, {message}";
                }

                SendEmail(completeMessage);
            }
        }
        else
        {
            throw new ArgumentException();
        }
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

            if (string.Equals(newISPAddress, currentISPAddress, StringComparison.CurrentCultureIgnoreCase))
            {
                oldISPAddress = currentISPAddress;
                currentISPAddress = newISPAddress;

                // ToDo create a email body if the ISP addres is changed
                throw new NotImplementedException();
                //SendEmail();

            }

        }
    }

    private void CreateBasicMailMessage()
    {
        // Set the sender, recipient, subject, and body of the message
        message.From = new MailAddress(_applicationSettingsOptions?.Value?.EmailFromAdress);
        message.To.Add(new MailAddress(_applicationSettingsOptions?.Value?.EmailToAdress));
        message.Subject = _applicationSettingsOptions?.Value?.EmailSubject;
        message.Priority = MailPriority.High;        
    }

    private void SendEmail(string emailBody)
    {
        if (_applicationSettingsOptions?.Value is not null)
        {
            // Create a new SmtpClient object within a using block
            using (SmtpClient client = new SmtpClient())
            {
                // Configure the SMTP client with your email provider's SMTP server address and credentials
                client.Host = _applicationSettingsOptions.Value.MailServer; ; // Replace with your SMTP server address
                client.Port = _applicationSettingsOptions.Value.SMTPPort; // Replace with your SMTP server port number
                client.UseDefaultCredentials = _applicationSettingsOptions.Value.UseDefaultCredentials; // If your SMTP server requires authentication, set this to false
                client.Credentials = new NetworkCredential(_applicationSettingsOptions?.Value?.UserName, _applicationSettingsOptions?.Value?.Password); // Replace with your SMTP server username and password
                client.EnableSsl = _applicationSettingsOptions.Value.EnableSsl; // Set this to true if your SMTP server requires SSL/TLS encryption               

                // Send the email message
                client.Send(message);

            }                   

        }
    }
}