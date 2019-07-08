using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace HTMLValidator.Models
{
    public class Report
    {
        public double Total { get; set; }
        public Dictionary<string, double> Coverage { get; set; }
    }
}
