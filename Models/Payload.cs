using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HTMLValidator.Models
{
    public static class Payload
    {
        public static async Task<string> Get(string url, HttpClient client, ILogger log)
        {
            try
            {
                log.LogInformation($"Starting request to {url}");
                var response = await client.GetAsync(url);
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (Exception ex)
            {
                log.LogInformation($"Failed to get payload for {url}: {ex}");
            }

            return string.Empty;
        }
    }
}
