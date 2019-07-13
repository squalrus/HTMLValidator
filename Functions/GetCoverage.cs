using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using HTMLValidator.Models;
using System.Linq;
using System.Collections.Generic;
using HTMLValidator.Extensions;
using Newtonsoft.Json;
using System.Text;

namespace HTMLValidator
{
    public static class GetCoverage
    {
        [FunctionName("GetCoverage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Blob("latest/cleaned.txt", FileAccess.Read)] Stream blob,
            [Blob("archive-coverage/{DateTime.Now}.json", FileAccess.Write)] Stream archiveCoverageBlob,
            [Table("coverage")] CloudTable cloudTable,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            StreamReader reader = new StreamReader(blob);
            string content = reader.ReadToEnd();
            var urlList = content.Split('\n');
            double totalPercentage = 0;
            int totalUrlList = 0;

            Report bundleData = new Report();
            bundleData.Coverage = new System.Collections.Generic.Dictionary<string, double>();

            List<string> filterSet = new List<string>();

            foreach (var url in urlList)
            {
                try
                {
                    var fullQuery = new TableQuery<Coverage>().Where(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, url.ToSlug())
                    ).Take(1);

                    var entity = (await cloudTable.ExecuteQuerySegmentedAsync(fullQuery, null)).Results;

                    if (entity != null)
                    {
                        if (!bundleData.Coverage.Keys.Contains(entity.First().PartitionKey))
                        {
                            bundleData.Coverage.Add(entity.First().PartitionKey, entity.First().Percent * 100);
                            totalPercentage += entity.First().Percent;
                            totalUrlList++;
                        }
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e, url);
                }
            }

            bundleData.Total = totalPercentage / totalUrlList * 100;
            bundleData.Urls = totalUrlList;
            archiveCoverageBlob.Write(Encoding.Default.GetBytes(JsonConvert.SerializeObject(bundleData)));
            return new JsonResult(bundleData);
        }
    }
}
