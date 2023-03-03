using CheckISPAdress.Models;
using CheckISPAdress.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;
using static CheckISPAdress.Options.ApplicationSettingsOptions;

namespace CheckISPAdress.Helpers
{
    public static class ConfigHelpers
    {
        public static bool DefaultMailSettingsChanged(IOptions<ApplicationSettingsOptions> _applicationSettingsOptions, ILogger logger)
        {
            bool configChanged = true;

            if (string.Equals(_applicationSettingsOptions?.Value?.MailServer, StandardAppsettingsValues.MailServer, StringComparison.CurrentCultureIgnoreCase))
            {
                configChanged = false;
                string errorMessage = "appsettings: MailServer not configured, this is for the mail you will recieve when the ISP adress is changed.";

                ThrowEmailConfigError(errorMessage, logger);
            }


            if (string.Equals(_applicationSettingsOptions?.Value?.UserName, StandardAppsettingsValues.UserName, StringComparison.CurrentCultureIgnoreCase))
            {
                configChanged = false;
                string errorMessage = "appsettings: UserName not configured, this is for the mail you will recieve when the ISP adress is changed.";


                ThrowEmailConfigError(errorMessage, logger);
            }


            if (!_applicationSettingsOptions!.Value.UseDefaultCredentials && string.Equals(_applicationSettingsOptions?.Value?.Password, StandardAppsettingsValues.Password, StringComparison.CurrentCultureIgnoreCase))
            {
                configChanged = false;
                string errorMessage = "appsettings: Password not configured, this is for the mail you will recieve when the ISP adress is changed.";

                ThrowEmailConfigError(errorMessage, logger);
            }


            if (string.Equals(_applicationSettingsOptions?.Value?.EmailToAdress, StandardAppsettingsValues.EmailToAdress, StringComparison.CurrentCultureIgnoreCase))
            {
                configChanged = false;
                string errorMessage = "EmailToAdress not configured, this is for the mail you will recieve when the ISP adress is changed.";

                ThrowEmailConfigError(errorMessage, logger);
            }


            if (string.Equals(_applicationSettingsOptions?.Value?.EmailFromAdress, StandardAppsettingsValues.EmailFromAdress, StringComparison.CurrentCultureIgnoreCase))
            {
                configChanged = false;
                string errorMessage = "appsettings:EmailFromAdress not confugured, this is for the mail you will recieve when the ISP adress is changed.";

                ThrowEmailConfigError(errorMessage, logger);
            }

            return configChanged;
        }
        public static ConfigErrorReportModel DefaultSettingsHaveBeenChanged(IOptions<ApplicationSettingsOptions> applicationSettingsOptions, ILogger logger)
        {
            ConfigErrorReportModel report = new();

            report.ChecksPassed = true;

            if (string.Equals(applicationSettingsOptions?.Value?.APIEndpointURL, StandardAppsettingsValues.APIEndpointURL, StringComparison.CurrentCultureIgnoreCase))
            {
                report.ChecksPassed = false;
                string errorMessage = "The APIEndpointURL is not changed, change the endpoint! (The endpoint of this API is: https://yourAPIURL//HTTP/GetIp)";

                ReportConfigError(errorMessage, logger);
                report.ErrorMessages.Add(errorMessage);
            }


            if (string.Equals(applicationSettingsOptions?.Value?.DNSRecordProviderURL, StandardAppsettingsValues.DNSRecordProviderURL, StringComparison.CurrentCultureIgnoreCase))
            {
                report.ChecksPassed = false;

                string errorMessage = "DNSRecordProviderURL in appsetting is not changed, this is for the mail you will recieve when the ISP adress is changed.";

                ReportConfigError(errorMessage, logger);
                report.ErrorMessages.Add(errorMessage);
            }

            if (string.Equals(applicationSettingsOptions?.Value?.EmailSubject, StandardAppsettingsValues.EmailSubject, StringComparison.CurrentCultureIgnoreCase))
            {
                report.ChecksPassed = false;

                string errorMessage = "EmailSubject in appsetting is not changed, this is for the mail you will recieve when the ISP adress is changed.";

                ReportConfigError(errorMessage, logger);
                report.ErrorMessages.Add(errorMessage);
            }

            return report;
        }

        private static void ReportConfigError(string errorMessage, ILogger logger)
        {
            Console.WriteLine(errorMessage);
            logger.LogInformation(errorMessage);
        }

        private static void ThrowEmailConfigError(string errorMessage, ILogger logger)
        {
            logger.LogInformation(errorMessage);
            Console.WriteLine(errorMessage);
            throw new Exception(errorMessage);
        }
    }
}
