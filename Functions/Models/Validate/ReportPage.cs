using System.Collections.Generic;

namespace HTMLValidator.Models.Validate
{
    public class ReportPage
    {
        public decimal Total { get; set; }
        public List<ReportModule> Modules { get; set; }
        public Dictionary<string, int> Classes { get; set; }
    }
}
