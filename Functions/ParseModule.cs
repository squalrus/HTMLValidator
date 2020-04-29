using HTMLValidator.Extensions;
using HTMLValidator.Models.GetCoverage;
using HTMLValidator.Models.Validate;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HTMLValidator
{
    public static class ParseModule
    {
        [FunctionName("ParseModule")]
        public static async Task Run(
            [TimerTrigger("0 0 1 * * *", RunOnStartup = true)] TimerInfo myTimer,
            [Blob("latest/cleaned.txt", FileAccess.Read)] Stream blob,
            [Table("coverage")] CloudTable cloudTable,
            [Table("modules{DateTime:yyyyMMdd}")] CloudTable nextModuleTable,
            ILogger log)
        {
            log.LogInformation("Starting ParseModule...");
            StreamReader reader = new StreamReader(blob);
            var content = reader.ReadToEnd();
            var urlList = content.Split('\n');

            Parallel.ForEach(urlList, async (url) =>
            {
                var query = new TableQuery<Coverage>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, url.ToSlug())
                ).Take(1);

                var entity = (await cloudTable.ExecuteQuerySegmentedAsync(query, null)).Results;
                IEnumerable<string> moduleList = null;

                try
                {
                    var report = JsonConvert.DeserializeObject<List<ReportModule>>(entity?.FirstOrDefault()?.Report);
                    moduleList = report.Where(x => !string.IsNullOrEmpty(x.Id)).Select(x => x.Id);
                }
                catch (Exception ex)
                {
                    log.LogInformation($"Failed to generate module list: {ex}");
                }

                log.LogInformation($"Processing modules for {url}...");
                foreach (var mod in moduleList)
                {
                    var updateOperation = TableOperation.InsertOrReplace(new TableEntity
                    {
                        PartitionKey = mod,
                        RowKey = url.ToSlug()
                    });
                    await nextModuleTable.ExecuteAsync(updateOperation);
                }
            });

            log.LogInformation("Clean up previous dataset...");
            var oldTableName = $"modules{DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd")}";
            var client = nextModuleTable.ServiceClient.GetTableReference(oldTableName);
            await client.DeleteIfExistsAsync();
            log.LogInformation("ParseModule complete!");
        }
    }
}
