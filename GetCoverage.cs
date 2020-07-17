using HTMLValidator.Extensions;
using HTMLValidator.Models.GetCoverage;
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            log.LogInformation("Starting GetCoverage...");
            StreamReader reader = new StreamReader(blob);
            var content = reader.ReadToEnd();
            var urlList = content.Split('\n');

            double overallCoverage = 0;
            int testedUrls = 0;
            var bundleData = new Report
            {
                Coverage = new Dictionary<string, Page>()
            };

            foreach (var url in urlList)
            {
                try
                {
                    log.LogInformation($"Processing URL {url.ToString()}");
                    var query = new TableQuery<Coverage>().Where(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, url.ToSlug())
                    ).Take(1);

                    var entity = (await cloudTable.ExecuteQuerySegmentedAsync(query, null)).Results;

                    if (entity != null)
                    {
                        if (!bundleData.Coverage.Keys.Contains(entity.First().PartitionKey))
                        {
                            var page = new Page
                            {
                                Coverage = entity.First().Percent * 100,
                                Url = url,
                                TestUrl = $"https://htmlvalidator.azurewebsites.net/api/ValidateUrl?url={url}"
                            };
                            bundleData.Coverage.Add(entity.First().PartitionKey, page);

                            overallCoverage += entity.First().Percent;
                            testedUrls++;
                        }
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e, url);
                }
            }

            bundleData.Total = overallCoverage / testedUrls * 100;
            bundleData.Urls = testedUrls;
            archiveCoverageBlob.Write(Encoding.Default.GetBytes(JsonConvert.SerializeObject(bundleData)));
            log.LogInformation("GetCoverage complete!");
            return new JsonResult(bundleData);
        }
    }
}
