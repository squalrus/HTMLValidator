using Microsoft.WindowsAzure.Storage.Table;

namespace HTMLValidator.Models.ParseClass
{
    public class ClassData : TableEntity
    {
        public int Count { get; set; }
    }
}
