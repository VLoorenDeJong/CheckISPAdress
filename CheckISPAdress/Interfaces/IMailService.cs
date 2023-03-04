namespace CheckISPAdress.Interfaces
{
    public interface IMailService
    {
        string ISPAddressChangedEmail(string newISPAddress, double interval, int requestCounter, int checkCounter);
        void SendEmail(string emailBody);
    }
}