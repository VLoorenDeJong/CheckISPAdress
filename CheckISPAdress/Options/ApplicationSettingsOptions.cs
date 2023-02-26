namespace CheckISPAdress.Options
{
    public class ApplicationSettingsOptions
    {
        public string? APIEndpointURL { get; set; }
        public string? HostingProviderURL { get; set; }
        public string? EmailAdress { get; set; }
        public double TimeIntervalInMinutes { get; set; }

        public class AppsettingsSections
        {
            public const string ApplicationSettings = "ApplicationSettings";
        }
    }
}
