using System.Collections.Generic;

namespace HTMLValidator.Models
{
    public class SchemaNode
    {
        public string Element { get; set; }
        public string[] Classes { get; set; }
        public SchemaNode[] ChildNodes { get; set; }
        public Dictionary<string, string[]> Attributes { get; set; }
    }
}
