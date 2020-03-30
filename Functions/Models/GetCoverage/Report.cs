using System.Collections.Generic;

namespace HTMLValidator.Models.GetCoverage
{
    public class Report
    {
        public double Total { get; set; }
        public int Urls { get; set; }
        public Dictionary<string, Page> Coverage { get; set; }
    }
}
