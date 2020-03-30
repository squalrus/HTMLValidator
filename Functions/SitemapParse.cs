using HTMLValidator.Models.SitemapParse;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HTMLValidator
{
    public static class SitemapParse
    {
        [FunctionName("SitemapParse")]
        public static async Task Run(
            [TimerTrigger("0 0 0 * * *", RunOnStartup = true)] TimerInfo myTimer,
            [Queue("urls"), StorageAccount("AzureWebJobsStorage")] IAsyncCollector<string> msg,
            [Blob("latest/full.txt", FileAccess.Write)] Stream latestFullBlob,
            [Blob("latest/cleaned.txt", FileAccess.Write)] Stream latestCleanedBlob,
            [Blob("latest/modules.txt", FileAccess.Write)] Stream latestModulesBlob,
            [Blob("archive-full/{DateTime.Now}.txt", FileAccess.Write)] Stream archiveFullBlob,
            [Blob("archive-cleaned/{DateTime.Now}.txt", FileAccess.Write)] Stream archiveCleanedBlob,
            ILogger log)
        {
            var azurecomUrl = "https://azure.microsoft.com/robotsitemap/en-us/{0}/";
            string sundogPayload = Payload.Get("https://sundog.azure.net/api/modules?status=1", log);

            List<string> urls = new List<string>();
            var completed = false;
            var pageNum = 1;

            while (!completed)
            {
                try
                {
                    string payload = Payload.Get(string.Format(azurecomUrl, pageNum), log);
                    log.LogInformation($"Processing page {pageNum}...");

                    var matches = Regex.Matches(payload, @"<loc>(.*?)<\/loc>");
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            urls.Add(match.Groups[1].Value);
                        }
                        log.LogInformation($"found {matches.Count} URLs.\n");
                    }
                    else
                    {
                        completed = true;
                        log.LogInformation($"found 0 URLs -- completed!\n");
                    }
                }
                catch (WebException e)
                {
                    Console.WriteLine(e);
                }

                pageNum++;
            }

            var distinctUrls = urls.Distinct();
            var cleanedUrls = distinctUrls.Where(x =>
                !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/blog\/(.*?)\/") &&
                (
                    !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/resources\/(.*?)\/") ||
                    (
                        x.Equals("https://azure.microsoft.com/en-us/resources/knowledge-center/") ||
                        x.Equals("https://azure.microsoft.com/en-us/resources/samples/") ||
                        x.Equals("https://azure.microsoft.com/en-us/resources/templates/") ||
                        x.Equals("https://azure.microsoft.com/en-us/resources/templates-partners/") ||
                        x.Equals("https://azure.microsoft.com/en-us/resources/videos/") ||
                        x.Equals("https://azure.microsoft.com/en-us/resources/whitepapers/") ||
                        x.Equals("https://azure.microsoft.com/en-us/resources/whitepapers/search/")
                    )
                ) &&
                !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/updates\/(.*?)\/") &&
                !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/resources\/videos\/(.*?)\/") &&
                !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/case-studies\/(.*?)") &&
                !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/community\/events\/(.*?)\/") &&
                !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/industries\/podcast\/(.*?)\/") &&
                !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/services\/open-datasets\/catalog\/(.*?)\/") &&
                !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/solutions\/architecture\/(.*?)\/"));


            log.LogInformation($"Writing to Blob Storage start.");
            latestFullBlob.Write(Encoding.Default.GetBytes(string.Join('\n', distinctUrls)));
            latestCleanedBlob.Write(Encoding.Default.GetBytes(string.Join('\n', cleanedUrls)));
            latestModulesBlob.Write(Encoding.Default.GetBytes(sundogPayload));
            archiveFullBlob.Write(Encoding.Default.GetBytes(string.Join('\n', distinctUrls)));
            archiveCleanedBlob.Write(Encoding.Default.GetBytes(string.Join('\n', cleanedUrls)));
            log.LogInformation($"Writing to Blob Storage complete.");

            log.LogInformation($"Queueing messages start.");
            Parallel.ForEach(cleanedUrls, async x => {
                await msg.AddAsync(x);
            });
            log.LogInformation($"Queueing messages complete.");
        }
    }
}
