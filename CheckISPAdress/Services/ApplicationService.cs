using CheckISPAdress.Interfaces;
using CheckISPAdress.Options;
using Microsoft.Extensions.Options;
using CheckISPAdress.Helpers;
using CheckISPAdress.Models;
using System.Runtime.CompilerServices;

namespace CheckISPAdress.Services
{
    public class ApplicationService : IApplicationService, IHostedService
    {
        private readonly ApplicationSettingsOptions _applicationSettingsOptions;
        private readonly ITimerService _timerService;
        private readonly IEmailService _emailService;
        private readonly IISPAdressCounterService _counterService;
        private readonly ILogger _logger;

        private bool configSuccess = false;

        public ApplicationService(ILogger<CheckISPAddressService> logger, IOptions<ApplicationSettingsOptions> applicationSettingsOptions, ITimerService timerService, IEmailService emailService, IISPAdressCounterService counterService)
        {
            _logger = logger;
            _applicationSettingsOptions = applicationSettingsOptions?.Value!;
            _timerService = timerService;
            _emailService = emailService;
            _counterService = counterService;

            configSuccess = CheckAppsettings();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            switch (configSuccess)
            {
                case true:
                    _logger.LogInformation("ApplicationService configSuccess: {configSuccess}", configSuccess);
                    _timerService!.StartISPCheckTimers();
                    break;
                case false:
                    _logger.LogInformation("ApplicationService configSuccess: {configSuccess}", configSuccess);
                    await StopAsync(default);
                    break;
            }           
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => _timerService!.Dispose());
        }
        private bool CheckAppsettings()
        {
            bool appsettingsConfigSuccess = true;

            if (_applicationSettingsOptions is not null)
            {
                bool mailConfigured = true;
                ConfigErrorReportModel report = new();

                mailConfigured = ConfigHelpers.MandatoryConfigurationChecks(_applicationSettingsOptions, _logger);

                switch (mailConfigured)
                {
                    case false:
                        appsettingsConfigSuccess = false;
                        return appsettingsConfigSuccess;
                    case true:
                        report = ConfigHelpers.DefaultSettingsCheck(_applicationSettingsOptions, _logger);
                        break;
                }

                switch (report.ChecksPassed)
                {
                    case false:
                        _emailService.SendConfigErrorMail(report.ErrorMessage!);
                        appsettingsConfigSuccess = false;
                        return appsettingsConfigSuccess;
                    case true:
                        _emailService.SendConfigSuccessMail(_counterService);
                        break;
                }
            }
            else
            {
                throw new ArgumentException();
            }

            return appsettingsConfigSuccess;
        }
    }    
}
