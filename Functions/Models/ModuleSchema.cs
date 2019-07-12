using Newtonsoft.Json.Linq;

namespace HTMLValidator.Models
{
    public class ModuleSchema
    {
        public string Slug { get; set; }
        public JObject Schema { get; set; }
    }
}
