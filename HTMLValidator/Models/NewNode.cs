using System.Collections.Generic;

namespace HTMLValidator.Models
{
    public class NewNode
    {
        public string Element { get; set; }
        public string[] Classes { get; set; }
        public NewNode[] ChildNodes { get; set; }
        public Dictionary<string, string[]> Attributes { get; set; }
    }
}
