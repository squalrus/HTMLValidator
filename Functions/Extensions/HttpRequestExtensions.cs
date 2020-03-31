using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace HTMLValidator.Extensions
{
    public static class HttpRequestExtensions
    {
        public static async Task<string> GetParameter(this HttpRequest req, string parameter)
        {
            string value = req.Query[parameter];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            return value ?? data?[parameter];
        }
    }
}
