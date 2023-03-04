namespace CheckISPAdress.Interfaces
{
    public interface IMailService
    {
        string CreateEmail(string emailMessage);
        string HeartBeatEmail();
        void SendEmail(string emailBody, string subject);
    }
}