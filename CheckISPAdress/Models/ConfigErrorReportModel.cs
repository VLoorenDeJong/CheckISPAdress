namespace CheckISPAdress.Models
{
    public class ConfigErrorReportModel
    {
        public bool ChecksPassed { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
    }
}
