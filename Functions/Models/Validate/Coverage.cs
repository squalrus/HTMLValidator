using Microsoft.WindowsAzure.Storage.Table;

namespace HTMLValidator.Models.Validate
{
    public class Coverage : TableEntity
    {
        public string Report { get; set; }
        public double Percent { get; set; }
        public string ClassList { get; set; }
    }
}
