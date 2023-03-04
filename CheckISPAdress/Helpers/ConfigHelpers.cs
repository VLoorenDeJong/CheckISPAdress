using CheckISPAdress.Models;
using CheckISPAdress.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using static CheckISPAdress.Options.ApplicationSettingsOptions;

namespace CheckISPAdress.Helpers
{
    public static class ConfigHelpers
    {
        public static bool MandatoryConfigurationChecks(ApplicationSettingsOptions _applicationSettingsOptions, ILogger logger)
        {
            bool MandatoryConfigurationPassed = true;

            if (string.Equals(_applicationSettingsOptions?.MailServer, StandardAppsettingsValues.MailServer, StringComparison.CurrentCultureIgnoreCase))
            {
                MandatoryConfigurationPassed = false;
                string errorMessage = "appsettings: MailServer in appsettings not configured, this is for the mail you will recieve when the ISP adress is changed.";

                ThrowEmailConfigError(errorMessage, logger);
            }


            if (string.Equals(_applicationSettingsOptions?.UserName, StandardAppsettingsValues.UserName, StringComparison.CurrentCultureIgnoreCase))
            {
                MandatoryConfigurationPassed = false;
                string errorMessage = "appsettings: UserName in appsettings not configured, this is for the mail you will recieve when the ISP adress is changed.";


                ThrowEmailConfigError(errorMessage, logger);
            }


            if (!_applicationSettingsOptions!.UseDefaultCredentials && string.Equals(_applicationSettingsOptions?.Password, StandardAppsettingsValues.Password, StringComparison.CurrentCultureIgnoreCase))
            {
                MandatoryConfigurationPassed = false;
                string errorMessage = "appsettings: Password in appsettings not configured, this is for the mail you will recieve when the ISP adress is changed.";

                ThrowEmailConfigError(errorMessage, logger);
            }


            if (string.Equals(_applicationSettingsOptions?.EmailToAdress, StandardAppsettingsValues.EmailToAdress, StringComparison.CurrentCultureIgnoreCase) || !EmailAddressIsValid(_applicationSettingsOptions?.EmailToAdress))
            {
                MandatoryConfigurationPassed = false;
                string errorMessage = $"appsettings: EmailToAdress: {_applicationSettingsOptions?.EmailToAdress} in appsettings not confugured correctly";

                ThrowEmailConfigError(errorMessage, logger);
            }


            if (string.Equals(_applicationSettingsOptions?.EmailFromAdress, StandardAppsettingsValues.EmailFromAdress, StringComparison.CurrentCultureIgnoreCase) || !EmailAddressIsValid(_applicationSettingsOptions?.EmailFromAdress))
            {
                MandatoryConfigurationPassed = false;
                string errorMessage = $"appsettings: EmailFromAdress: {_applicationSettingsOptions?.EmailFromAdress} in appsettings not confugured correctly";

                ThrowEmailConfigError(errorMessage, logger);
            }

            if (_applicationSettingsOptions?.BackupAPIS is null || BackupAPIUrlError(_applicationSettingsOptions?.BackupAPIS, logger))
            {

                MandatoryConfigurationPassed = false;
                string errorMessage = "appsettings: BackupAPIS in appsettings not confugured, this is for checking for you ISP adress when the ISP adress is changed.";

                ThrowEmailConfigError(errorMessage, logger);
            }

            return MandatoryConfigurationPassed;
        }

        private static bool EmailAddressIsValid(string? emailAdressToValidate)
        {
            bool isVallid = false;
            
            if (!string.IsNullOrWhiteSpace(emailAdressToValidate))
            {
                isVallid = Regex.IsMatch(emailAdressToValidate!, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            }

            return isVallid;
        }

        private static bool BackupAPIUrlError(List<string?>? backupAPIURLs, ILogger logger)
        {
            bool urlConfigError = false;

            if (backupAPIURLs is not null)
            {
                foreach (string? APIUrl in backupAPIURLs)
                {
                    if (string.IsNullOrWhiteSpace(APIUrl) || !Regex.IsMatch(APIUrl, @"^https?:\/\/[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?$"))
                    {
                        urlConfigError = true;
                        string errorMessage = $"appsettings: Backup API URL not correct, index: {backupAPIURLs.IndexOf(APIUrl)}, URL: {APIUrl}";
                        ThrowEmailConfigError(errorMessage, logger);
                    }
                }
            }

            return urlConfigError;
        }

        public static ConfigErrorReportModel DefaultSettingsHaveBeenChanged(ApplicationSettingsOptions applicationSettingsOptions, ILogger logger)
        {
            ConfigErrorReportModel report = new();

            report.ChecksPassed = true;

            if (string.Equals(applicationSettingsOptions?.APIEndpointURL, StandardAppsettingsValues.APIEndpointURL, StringComparison.CurrentCultureIgnoreCase))
            {
                report.ChecksPassed = false;
                string errorMessage = "appsettings: The APIEndpointURL in appsettings is not changed, change the endpoint! (The endpoint of this API is: https://yourAPIURL//HTTP/GetIp) <br />";

                ReportConfigError(errorMessage, logger);
                report.ErrorMessages.Add(errorMessage);
            }


            if (string.Equals(applicationSettingsOptions?.DNSRecordHostProviderURL, StandardAppsettingsValues.DNSRecordHostProviderURL, StringComparison.CurrentCultureIgnoreCase))
            {
                report.ChecksPassed = false;

                string errorMessage = "appsettings: DNSRecordProviderURL in appsetting is not changed, this is for the mail you will recieve when the ISP adress is changed. <br />";

                ReportConfigError(errorMessage, logger);
                report.ErrorMessages.Add(errorMessage);
            }

            if (string.Equals(applicationSettingsOptions?.EmailSubject, StandardAppsettingsValues.EmailSubject, StringComparison.CurrentCultureIgnoreCase))
            {
                report.ChecksPassed = false;

                string errorMessage = "appsettings: EmailSubject in appsetting is not changed, this is for the mail you will recieve when the ISP adress is changed. <br />";

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
