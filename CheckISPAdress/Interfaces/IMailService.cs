namespace CheckISPAdress.Interfaces
{
    public interface IMailService
    {
        void SendConfigSuccessMail(string newISPAddress, IISPAdressCounterService counterService, double interval);
        void SendConnectionReestablishedEmail(string newISPAddress, string oldISPAddress, IISPAdressCounterService counterService, double interval);
        void SendCounterDifferenceEmail(IISPAdressCounterService counterService);
        void SendDifferendISPAdressValuesEmail(Dictionary<string, string> externalISPAdressChecks, string oldISPAddress, IISPAdressCounterService counterService, double interval);
        void SendExternalAPIExceptionEmail(string APIUrl, string exceptionType, string exceptionMessage);
        void SendExternalAPIHTTPExceptionEmail(string APIUrl, string exceptionType, string exceptionMessage);
        void SendHeartBeatEmail( IISPAdressCounterService counterService, string oldISPAddress, string currentISPAddress, string newISPAddress, Dictionary<string, string> externalISPCheckResults);
        void SendISPAdressChangedEmail(string externalISPAddress, string oldISPAddress, IISPAdressCounterService counterService, double interval);
        void SendISPAPIExceptionEmail(string exceptionType, string exceptionMessage);
        void SendNoISPAdressReturnedEmail(string oldISPAddress, IISPAdressCounterService counterService, double interval);
        void SendISPAPIHTTPExceptionEmail(string exceptionType, string exceptionMessage);
    }
}