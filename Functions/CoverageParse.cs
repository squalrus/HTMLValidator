using HTMLValidator.Extensions;
using HTMLValidator.Models.CoverageParse;
using HTMLValidator.Models.Validate;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HTMLValidator
{
    public static class CoverageParse
    {
        [FunctionName("CoverageParse")]
        public static async Task Run(
            [TimerTrigger("0 0 1 * * *")] TimerInfo myTimer,
            [Blob("latest/cleaned.txt", FileAccess.Read)] Stream blob,
            [Blob("archive-coverage/{DateTime.Now}.json", FileAccess.Write)] Stream archiveCoverageBlob,
            [Table("coverage")] CloudTable cloudTable,
            [Table("coverage{DateTime:yyyyMMdd}")] CloudTable nextCoverageTable,
            ILogger log)
        {
            log.LogInformation("Starting CoverageParse...");
            StreamReader reader = new StreamReader(blob);
            var content = reader.ReadToEnd();
            var urlList = content.Split('\n');

            foreach (var url in urlList)
            {
                try
                {
                    var query = new TableQuery<Coverage>().Where(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, url.ToSlug())
                    ).Take(1);

                    var entity = (await cloudTable.ExecuteQuerySegmentedAsync(query, null)).Results;

                    log.LogInformation($"Processing classes for {url}...");
                    if (entity != null)
                    {
                        var updateOperation = TableOperation.InsertOrReplace(new Page
                        {
                            PartitionKey = "coverage",
                            RowKey = url.ToSlug(),
                            Coverage = entity.First().Percent * 100,

                        });
                        await nextCoverageTable.ExecuteAsync(updateOperation);
                    }
                }
                catch (Exception ex)
                {
                    log.LogError($"Failed to get data for {url}: {ex}");
                }
            }

            log.LogInformation("Clean up previous dataset...");
            var oldTableName = $"coverage{DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd")}";
            var client = nextCoverageTable.ServiceClient.GetTableReference(oldTableName);
            await client.DeleteIfExistsAsync();
            log.LogInformation("CoverageParse complete!");
        }
    }
}
