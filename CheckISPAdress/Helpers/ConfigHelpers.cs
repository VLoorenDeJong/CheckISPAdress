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
                string errorMessage = "appsettings: BackupAPIs in appsettings not confugured, this is for checking for you ISP adress when the ISP adress is changed.";

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

                string errorMessage = $"<p><h5><strong>appsettings:</strong></h5></p>"
                                    + $"<p>The <strong>APIEndpointURL</strong> in appsettings is not changed</p>"
                                    + "<p>change the endpoint!</p>"
                                    + $"<p>The endpoint of this API is:</p>"
                                    + $"<p><strong>{applicationSettingsOptions?.APIEndpointURL}<strong></p>";

                ReportConfigError(errorMessage, logger);
                report.ErrorMessage = report.ErrorMessage + errorMessage;
            }


            if (string.Equals(applicationSettingsOptions?.DNSRecordHostProviderURL, StandardAppsettingsValues.DNSRecordHostProviderURL, StringComparison.CurrentCultureIgnoreCase))
            {
                report.ChecksPassed = false;

                string errorMessage = $"<p><h5><strong>appsettings:</strong></h5></p>"
                                    + $"<p>The <strong> DNSRecordProviderURL </strong> in appsettings is not changed</p>"
                                    + "<p>this is for the mail you will recieve when the ISP adress is changed.</p>";

                ReportConfigError(errorMessage, logger);
                report.ErrorMessage = report.ErrorMessage + errorMessage;
            }

            if (string.Equals(applicationSettingsOptions?.EmailSubject, StandardAppsettingsValues.EmailSubject, StringComparison.CurrentCultureIgnoreCase))
            {
                report.ChecksPassed = false;

                string errorMessage = $"<p><h5><strong>appsettings:</strong></h5></p>"
                                    + $"<p>The <strong> EmailSubject </strong> in appsettings is not changed</p>"
                                    + "<p>this is for the mail you will recieve when the ISP adress is changed.</p>";

                ReportConfigError(errorMessage, logger);
                report.ErrorMessage = report.ErrorMessage + errorMessage;
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
