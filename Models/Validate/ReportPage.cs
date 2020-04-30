using System.Collections.Generic;

namespace HTMLValidator.Models.Validate
{
    public class ReportPage
    {
        public double Total { get; set; }
        public List<ReportModule> Modules { get; set; }
        public Dictionary<string, int> Classes { get; set; }
    }
}
