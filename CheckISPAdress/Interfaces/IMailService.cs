namespace CheckISPAdress.Interfaces
{
    public interface IMailService
    {
        string ISPAddressChangedEmail(string newISPAddress, string oldISPAddress, double interval, int requestCounter, int checkCounter);
        void SendEmail(string emailBody);
    }
}