using CheckISPAdress.Interfaces;

namespace CheckISPAdress.Services
{

    public class ISPAdressCounterService : IISPAdressCounterService
    {
        public int ISPEndpointRequests { get; set; }
        public int ServiceRequestCounter { get; set; } = 0;
        public int ServiceCheckCounter { get; set; } = 1;
        public int FailedISPRequestCounter { get; set; } = 0;
    }
}