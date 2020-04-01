using Microsoft.WindowsAzure.Storage.Table;

namespace HTMLValidator.Models.ClassParse
{
    public class ClassData : TableEntity
    {
        public int Count { get; set; }
    }
}
