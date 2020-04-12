using Microsoft.WindowsAzure.Storage.Table;

namespace HTMLValidator.Models.CoverageParse
{
    public class Page : TableEntity
    {
        public double Coverage { get; set; }
    }
}
