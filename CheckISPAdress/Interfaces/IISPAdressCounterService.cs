namespace CheckISPAdress.Interfaces
{
    public interface IISPAdressCounterService
    {
        void AddExternalServiceCheckCounter();
        void AddFailedISPRequestCounter();
        void AddISPEndpointRequests();
        void AddServiceCheckCounter();
        void AddServiceRequestCounter();
        int GetExternalServiceCheckCounter();
        int GetFailedISPRequestCounter();
        int GetInternalAPICallsCounter();
        int GetServiceCheckCounter();
        int GetServiceRequestCounter();
        void ResetFailedISPRequestCounter();
    }
}