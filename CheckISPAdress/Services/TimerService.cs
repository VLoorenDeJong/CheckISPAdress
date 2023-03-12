using Microsoft.Extensions.Options;
using CheckISPAdress.Interfaces;
using CheckISPAdress.Helpers;
using CheckISPAdress.Options;
using CheckISPAdress.Models;

namespace CheckISPAdress.Services
{
    public class TimerService : ITimerService

    {
        private readonly ApplicationSettingsOptions _applicationSettingsOptions;
        private readonly ICheckISPAddressService _ISPAdressService;
        private readonly IISPAdressCounterService _counterService;
        private readonly ILogger _logger;

        private Timer? controlISPAdressCheckTimer;
        private Timer? ISPAdressCheckTimer;
        private Timer? HeartbeatemailTimer;

        private double ISPAdressCHeckInterval;

        public TimerService(ILogger<CheckISPAddressService> logger, IOptions<ApplicationSettingsOptions> applicationSettingsOptions, IISPAdressCounterService counterService, ICheckISPAddressService ISPAdressService)
        {
            _logger = logger;
            _applicationSettingsOptions = applicationSettingsOptions?.Value!;
            _counterService = counterService;
            _ISPAdressService = ISPAdressService;
        }

        public void StartISPCheckTimers()
        {
            ISPAdressCHeckInterval = (_applicationSettingsOptions.TimeIntervalInMinutes == 0) ? 60 : _applicationSettingsOptions.TimeIntervalInMinutes;

            ISPAdressCheckTimer = new Timer(async (state) => await _ISPAdressService.GetISPAddressAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(ISPAdressCHeckInterval));
            controlISPAdressCheckTimer = new Timer(state => { _counterService.AddServiceCheckCounter(); }, null, TimeSpan.FromMinutes(ISPAdressCHeckInterval), TimeSpan.FromMinutes(ISPAdressCHeckInterval));
            SetupHeartbeatTimer();
        }

        private void SetupHeartbeatTimer()
        {
            DateTime now = DateTime.Now;
            DateTime nextOccurrence = now.AddDays(((int)_applicationSettingsOptions.HeatbeatEmailDayOfWeek - (int)now.DayOfWeek + 7) % 7).Date.Add(_applicationSettingsOptions.HeatbeatEmailTimeOfDay);

            if (nextOccurrence < now)
            {
                nextOccurrence = nextOccurrence.AddDays(_applicationSettingsOptions.HeatbeatEmailIntervalDays);
            }

            TimeSpan heartBeatInterval = TimeSpan.FromDays(_applicationSettingsOptions.HeatbeatEmailIntervalDays);

            HeartbeatemailTimer = new Timer(async (state) =>
            {
                // Do something here when the timer elapses, such as calling an async method
                await _ISPAdressService.HeartBeatCheck();
            }, null, (int)(nextOccurrence - now).TotalMilliseconds, (int)heartBeatInterval.TotalMilliseconds);
        }

        public void Dispose()
        {
            ISPAdressCheckTimer!.Dispose();
            controlISPAdressCheckTimer!.Dispose();
            HeartbeatemailTimer!.Dispose();
        }
    }
}
