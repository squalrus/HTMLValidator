using HTMLValidator.Extensions;
using HTMLValidator.Models;
using HTMLValidator.Models.Validate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace HTMLValidator
{
    public static class ValidateUrl
    {
        [FunctionName("ValidateUrl")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing ValidateUrl.");
            var testUrl = await req.GetParameter("url");
            ModuleSchema[] schema = null;
            ReportPage report = null;

            try
            {
                string payload = Payload.Get("https://sundog.azure.net/api/modules?status=1", log);
                schema = JsonConvert.DeserializeObject<ModuleSchema[]>(payload);
            }
            catch (Exception ex)
            {
                log.LogInformation($"Failed to generate module schema: {ex}");
            }

            try
            {
                string html = Payload.Get(testUrl, log);
                report = new Validator(html, schema).Process();
            }
            catch (WebException ex)
            {
                log.LogInformation($"Failed to validate page: {ex}");
            }

            return testUrl != null
                ? new JsonResult(report, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }
                )
                 : new JsonResult("Please pass a url on the query string or in the request body");
        }
    }
}
