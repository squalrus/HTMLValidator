using HTMLValidator.Extensions;
using HTMLValidator.Models.GetCoverage;
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
            [Blob("archive-coverage/{DateTime.Now}.json", FileAccess.Write)] Stream archiveCoverageBlob,
            [Table("coverage{DateTime:yyyyMMdd}")] CloudTable cloudTable,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            double totalCoverage = 0;
            var bundleData = new Report
            {
                Coverage = new Dictionary<string, Page>()
            };
            List<Models.CoverageParse.Page> entity = new List<Models.CoverageParse.Page>();

            try
            {
                var query = new TableQuery<HTMLValidator.Models.CoverageParse.Page>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "coverage")
                );

                entity = (await cloudTable.ExecuteQuerySegmentedAsync(query, null)).Results;

                foreach (var item in entity)
                {
                    if (!bundleData.Coverage.Keys.Contains(item.RowKey))
                    {
                        var page = new HTMLValidator.Models.GetCoverage.Page
                        {
                            Coverage = item.Coverage,
                            Url = item.RowKey.ToUrl(),
                            TestUrl = $"https://htmlvalidator.azurewebsites.net/api/ValidateUrl?url={item.RowKey.ToUrl()}"
                        };
                        bundleData.Coverage.Add(item.RowKey, page);
                        totalCoverage += item.Coverage;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            bundleData.Total = totalCoverage / entity.Count;
            bundleData.Urls = entity.Count;
            archiveCoverageBlob.Write(Encoding.Default.GetBytes(JsonConvert.SerializeObject(bundleData)));
            return new JsonResult(bundleData);
        }
    }
}
