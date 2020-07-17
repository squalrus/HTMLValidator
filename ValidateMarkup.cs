using HTMLValidator.Models;
using HTMLValidator.Models.Validate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace HTMLValidator
{
    public static class ValidateMarkup
    {
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("ValidateMarkup")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing ValidateMarkup.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var requestClean = requestBody.Replace("markup=", "").Replace("\"", "");
            var html = HttpUtility.UrlDecode(requestClean);
            ModuleSchema[] schema = null;
            ReportPage report = null;

            try
            {
                string payload = await Payload.Get("https://sundog.azure.net/api/modules?status=1", httpClient, log);
                schema = JsonConvert.DeserializeObject<ModuleSchema[]>(payload);
            }
            catch (Exception ex)
            {
                log.LogInformation($"Failed to generate module schema: {ex}");
            }

            try
            {
                report = new Validator(html, schema).Process();
            }
            catch (WebException ex)
            {
                log.LogInformation($"Failed to validate markup: {ex}");
            }

            return html != null
                ? new JsonResult(report, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }
                )
                : new JsonResult("Please pass markup in the request body");
        }
    }
}
