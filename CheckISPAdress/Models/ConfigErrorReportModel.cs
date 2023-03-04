namespace CheckISPAdress.Models
{
    public class ConfigErrorReportModel
    {
        public bool ChecksPassed { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }
}
