using Newtonsoft.Json.Linq;

namespace HTMLValidator.Models.Validate
{
    public class ModuleSchema
    {
        public string Slug { get; set; }
        public JObject Schema { get; set; }
    }
}
