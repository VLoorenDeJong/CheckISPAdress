namespace CheckISPAdress.Interfaces
{
    public interface IISPAdressCounterService
    {
        void AddFailedISPRequestCounter();
        void AddISPEndpointRequests();
        void AddServiceCheckCounter();
        void AddServiceRequestCounter();
        int GetFailedISPRequestCounter();
        int GetISPEndpointRequests();
        int GetServiceCheckCounter();
        int GetServiceRequestCounter();
        void ResetFailedISPRequestCounter();
    }
}