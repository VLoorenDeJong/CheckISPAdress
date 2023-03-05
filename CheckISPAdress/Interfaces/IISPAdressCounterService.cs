namespace CheckISPAdress.Interfaces
{
    public interface IISPAdressCounterService
    {
        int FailedISPRequestCounter { get; set; }
        int ISPEndpointRequests { get; set; }
        int ServiceCheckCounter { get; set; }
        int ServiceRequestCounter { get; set; }
    }
}