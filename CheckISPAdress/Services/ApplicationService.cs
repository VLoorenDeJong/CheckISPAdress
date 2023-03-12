using CheckISPAdress.Interfaces;
using CheckISPAdress.Options;
using Microsoft.Extensions.Options;
using CheckISPAdress.Helpers;
using CheckISPAdress.Models;

namespace CheckISPAdress.Services
{
    public class ApplicationService : IApplicationService, IHostedService
    {
        private readonly ApplicationSettingsOptions _applicationSettingsOptions;
        private readonly ITimerService _timerService;
        private readonly IMailService _emailService;
        private readonly ILogger _logger;

        public ApplicationService(ILogger<CheckISPAddressService> logger, IOptions<ApplicationSettingsOptions> applicationSettingsOptions, ITimerService timerService, IMailService emailService)
        {
            _logger = logger;
            _applicationSettingsOptions = applicationSettingsOptions?.Value!;
            _timerService = timerService;
            _emailService = emailService;

            CheckAppsettings();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            bool mandatorySettingsOk = ConfigHelpers.MandatoryConfigurationChecks(_applicationSettingsOptions, _logger);
            _logger.LogInformation("mandatorySettingsOk: {mandatorySettingsOk}", mandatorySettingsOk);

            if (mandatorySettingsOk)
            {
                ConfigErrorReportModel defaultSettingsReport;

                defaultSettingsReport = ConfigHelpers.DefaultSettingsCheck(_applicationSettingsOptions, _logger);

                if (defaultSettingsReport!.ChecksPassed)
                {
                    _logger.LogInformation("defaultSettingsReport!.ChecksPassed: {ChecksPassed}", defaultSettingsReport!.ChecksPassed);
                    _timerService!.StartISPCheckTimers();
                }
                else
                {
                    _logger.LogInformation("defaultSettingsReport!.ChecksPassed: {ChecksPassed}", defaultSettingsReport!.ChecksPassed);
                    await StopAsync(default);
                }
            }
            else
            {
                await StopAsync(default);
                throw new Exception("Review appsettings");

            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => _timerService!.Dispose());
        }
        private void CheckAppsettings()
        {
            if (_applicationSettingsOptions is not null)
            {
                bool mailConfigured = true;
                ConfigErrorReportModel report = new();

                mailConfigured = ConfigHelpers.MandatoryConfigurationChecks(_applicationSettingsOptions, _logger);
                if (mailConfigured) _emailService.();
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
    }    
}
