using HTMLValidator.Extensions;
using HTMLValidator.Models.ClassParse;
using HTMLValidator.Models.Validate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HTMLValidator
{
    public static class GetClass
    {
        [FunctionName("GetClass")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Table("classes{DateTime:yyyyMMdd}")] CloudTable nextClassTable,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var className = await req.GetParameter("class");
            var report = new ReportClass();

            try
            {
                var query = new TableQuery<Coverage>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, className)
                );

                var entity = new List<Coverage>();

                if (await nextClassTable.ExistsAsync())
                {
                    entity = (await nextClassTable.ExecuteQuerySegmentedAsync(query, null)).Results;
                }
                else
                {
                    var oldTableName = $"classes{DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd")}";
                    var client = nextClassTable.ServiceClient.GetTableReference(oldTableName);
                    entity = (await client.ExecuteQuerySegmentedAsync(query, null)).Results;
                }

                report.Urls = entity.Select(x => x.RowKey.ToUrl()).ToList();
                report.Total = entity.Count();
            }
            catch (Exception ex)
            {
                log.LogInformation($"Failed to get class data: {ex}");
            }

            return new JsonResult(report, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }
}
