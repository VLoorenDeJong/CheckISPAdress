namespace CheckISPAdress.Options
{
    public class ApplicationSettingsOptions
    {
        public string? APIEndpointURL { get; set; }
        public double TimeIntervalInMinutes { get; set; }
        public string? DNSRecordProviderURL { get; set; }
        public string? EmailToAdress { get; set; }
        public string? EmailFromAdress { get; set; }
        public string? EmailSubject { get; set; }
        public string? EmailHost { get; set; }
        public int SMTPPort { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string? userName { get; set; }
        public string? password { get; set; }
        public bool EnableSsl { get; set; }

        public class AppsettingsSections
        {
            public const string ApplicationSettings = "ApplicationSettings";
        }

        public class StandardAppsettingsValues 
        {
            public const string APIEndpointURL = "https://api.ipify.org";
            public const string HostingProviderURL = "www.YourHostingProviderGoesHere.YAY";
            public const string EmailFromAdress = "Your@API.com";
            public const string EmailToAdress = "YourEmailAdress@here.com";
            public const string EmailSubject = "YourEmailSubject";
            public const string TimeIntervalInMinutes = "https://api1.victorloorendejong.nl//HTTP/GetIp";
            public const string EmailHost = "smtp.example.com";
            public const string UserName = "UserName";
            public const string password = "Pa$$w0rd";

        }
    }
}
