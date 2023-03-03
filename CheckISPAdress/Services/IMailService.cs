namespace CheckISPAdress.Services
{
    public interface IMailService
    {
        string ISPAddressChangedEmail(string newISPAddress, string oldISPAddress, string? dNSRecordHostingProviderURL, double interval, int requestCounter, int checkCounter);
        void SendEmail(string emailBody);
    }
}