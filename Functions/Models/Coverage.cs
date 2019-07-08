using Microsoft.WindowsAzure.Storage.Table;

namespace HTMLValidator.Models
{
    public class Coverage : TableEntity
    {
        public string Report { get; set; }
        public double Percent { get; set; }
    }
}
