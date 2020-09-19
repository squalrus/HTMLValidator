using HTMLValidator.Extensions;
using HTMLValidator.Models.ParseClass;
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
    public static class ParseClass
    {
        [FunctionName("ParseClass")]
        public static async Task Run(
            [TimerTrigger("0 0 1 * * *")] TimerInfo myTimer,
            [Blob("latest/cleaned.txt", FileAccess.Read)] Stream blob,
            [Table("coverage")] CloudTable cloudTable,
            [Table("classes{DateTime:yyyyMMdd}")] CloudTable nextClassTable,
            ILogger log)
        {
            log.LogInformation("Starting ParseClass...");
            StreamReader reader = new StreamReader(blob);
            var content = reader.ReadToEnd();
            var urlList = content.Split('\n');

            Parallel.ForEach(urlList, async (url) =>
            {
                var query = new TableQuery<Coverage>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, url.ToSlug())
                ).Take(1);

                var entity = (await cloudTable.ExecuteQuerySegmentedAsync(query, null)).Results;
                Dictionary<string, int> classList = null;

                try
                {
                    classList = JsonConvert.DeserializeObject<Dictionary<string, int>>(entity?.FirstOrDefault()?.ClassList);
                }
                catch (Exception ex)
                {
                    log.LogInformation($"Failed to generate class list: {ex}");
                }

                log.LogInformation($"Processing classes for {url}...");
                foreach (var cls in classList)
                {
                    var updateOperation = TableOperation.InsertOrReplace(new ClassData
                    {
                        PartitionKey = cls.Key,
                        RowKey = url.ToSlug(),
                        Count = cls.Value
                    });
                    await nextClassTable.ExecuteAsync(updateOperation);
                }
            });

            log.LogInformation("Clean up previous dataset...");
            var oldTableName = $"classes{DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd")}";
            var client = nextClassTable.ServiceClient.GetTableReference(oldTableName);
            await client.DeleteIfExistsAsync();
            log.LogInformation("ParseClass complete!");
        }
    }
}
