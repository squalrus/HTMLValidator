using HTMLValidator.Extensions;
using HTMLValidator.Models.ParseClass;
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
    public static class GetModule
    {
        [FunctionName("GetModule")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Table("modules{DateTime:yyyyMMdd}")] CloudTable nextModuleTable,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var moduleName = await req.GetParameter("module");
            var report = new ReportClass();

            try
            {
                var query = new TableQuery<Coverage>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, moduleName)
                );

                var entity = new List<Coverage>();

                if (await nextModuleTable.ExistsAsync())
                {
                    entity = (await nextModuleTable.ExecuteQuerySegmentedAsync(query, null)).Results;
                }
                else
                {
                    var oldTableName = $"modules{DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd")}";
                    var client = nextModuleTable.ServiceClient.GetTableReference(oldTableName);
                    entity = (await client.ExecuteQuerySegmentedAsync(query, null)).Results;
                }

                report.Urls = entity.Select(x => x.RowKey.ToUrl()).ToList();
                report.Total = entity.Count();
            }
            catch (Exception ex)
            {
                log.LogInformation($"Failed to get module data: {ex}");
            }

            return new JsonResult(report);
        }
    }
}
