using Newtonsoft.Json.Linq;

namespace HTMLValidator.Models
{
    public class Schema
    {
        public string Slug { get; set; }
        public JObject JsonSchema { get; set; }
    }
}
