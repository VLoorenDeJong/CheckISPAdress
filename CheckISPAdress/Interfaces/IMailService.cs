namespace CheckISPAdress.Interfaces
{
    public interface IMailService
    {
        string CreateEmail(string emailMessage);
        string HeartBeatEmail();
        void SendConfigSuccessMail(string newISPAddress, IISPAdressCounterService _counterService, double interval);
        void SendConnectionReestablishedEmail(string newISPAddress, string oldISPAddress, IISPAdressCounterService _counterService, double interval);
        void SendCounterDifferenceEmail(IISPAdressCounterService _counterService);
        void SendDifferendISPAdressValuesEmail(Dictionary<string, string> ISPAdressChecks, string oldISPAddress, IISPAdressCounterService counterService, double interval);
        void SendEmail(string emailBody, string subject);
        void SendExternalAPIExceptionEmail(string APIUrl, string exceptionType, string exceptionMessage);
        void SendExternalAPIHTTPExceptionEmail(string APIUrl, string exceptionType, string exceptionMessage);
        void SendISPAdressChangedEmail(string externalISPAddress, string oldISPAddress, IISPAdressCounterService counterService, double interval);
        void SendISPAPIEceptionEmail(string exceptionType, string exceptionMessage);
        void SendNoISPAdressReturnedEmail(string oldISPAddress, IISPAdressCounterService counterService, double interval);
        void SenISPAPIHTTPExceptionEmail(string exceptionType, string exceptionMessage);
    }
}