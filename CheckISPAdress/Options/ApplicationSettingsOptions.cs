namespace CheckISPAdress.Options
{
    public class ApplicationSettingsOptions
    {
        public string? APIEndpointURL { get; set; }
        public List<string?>? BackupAPIS { get; set; }
        public double TimeIntervalInMinutes { get; set; }
        public string? DNSRecordHostProviderName { get; set; }
        public string? DNSRecordHostProviderURL { get; set; }
        public string? EmailToAdress { get; set; }
        public string? EmailFromAdress { get; set; }
        public string? EmailSubject { get; set; }
        public string? DateTimeFormat { get; set; }
        public string? MailServer { get; set; }
        public int SMTPPort { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public bool EnableSsl { get; set; }

        public class AppsettingsSections
        {
            public const string ApplicationSettings = "ApplicationSettings";
        }

        public class StandardAppsettingsValues 
        {
            public const string APIEndpointURL = "ThisAPIEnpointURL";
            public const string DNSRecordHostProviderName = "HostingProviderName";
            public const string DNSRecordHostProviderURL = "HostingProviderURL";
            public const string EmailFromAdress = "EmailFromAdress";
            public const string EmailToAdress = "EmailToAdress";
            public const string EmailSubject = "YourEmailSubject";
            public const string MailServer = "MailServer";
            public const string UserName = "UserName";
            public const string Password = "Pa$$w0rd";

        }
    }
}
