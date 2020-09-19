using HTMLValidator.Extensions;
using HTMLValidator.Models;
using HTMLValidator.Models.Validate;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HTMLValidator
{
    public static class ValidateUrlMessage
    {
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("ValidateUrlMessage")]
        [return: Table("coverage")]
        public static async Task<Coverage> Run(
            [QueueTrigger("urls", Connection = "AzureWebJobsStorage")] string myQueueItem,
            [Blob("latest/modules.txt", FileAccess.Read)] string modulePayload,
            ILogger log)
        {
            log.LogInformation("Processing ValidateUrlMessage.");

            string slug = myQueueItem.ToSlug();
            string reverseTicks = (DateTime.MaxValue.Ticks - DateTime.Now.Ticks).ToString();
            ModuleSchema[] schema = null;
            ReportPage report = null;

            try
            {
                schema = JsonConvert.DeserializeObject<ModuleSchema[]>(modulePayload);
            }
            catch (Exception ex)
            {
                log.LogInformation($"Failed to generate module schema: {ex}");
            }

            try
            {
                string html = await Payload.Get(myQueueItem, httpClient, log);
                report = await new Validator(html, schema, httpClient, log).Process();

                return new Coverage
                {
                    PartitionKey = slug,
                    RowKey = reverseTicks,
                    Report = JsonConvert.SerializeObject(report.Modules),
                    ClassList = JsonConvert.SerializeObject(report.Classes),
                    Percent = (double)report.Total
                };
            }
            catch (WebException ex)
            {
                log.LogInformation($"Failed to validate message: {ex}");
            }

            return new Coverage
            {
                PartitionKey = slug,
                RowKey = reverseTicks,
                Report = "Parsing failure",
                ClassList = null,
                Percent = 0
            };
        }
    }
}
