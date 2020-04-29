namespace HTMLValidator.Models.Validate
{
    public class ReportModule
    {
        public string Id { get; }
        public string Status { get; }
        public string Details { get; }

        public ReportModule(string id, string status, string details = null)
        {
            Id = id;
            Status = status;
            Details = details;
        }
    }
}
