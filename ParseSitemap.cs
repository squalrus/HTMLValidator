using HTMLValidator.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HTMLValidator
{
    public static class ParseSitemap
    {
        private static HttpClient httpClient = new HttpClient();

        [Disable]
        [FunctionName("ParseSitemap")]
        public static async Task Run(
            [TimerTrigger("0 0 0 * * *", RunOnStartup = true)] TimerInfo myTimer,
            [Queue("urls"), StorageAccount("AzureWebJobsStorage")] IAsyncCollector<string> msg,
            [Blob("latest/full.txt", FileAccess.Write)] Stream latestFullBlob,
            [Blob("latest/cleaned.txt", FileAccess.Write)] Stream latestCleanedBlob,
            [Blob("latest/modules.txt", FileAccess.Write)] Stream latestModulesBlob,
            [Blob("latest/custom.txt", FileAccess.Read)] Stream customUrlsBlob,
            [Blob("archive-full/{DateTime.Now}.txt", FileAccess.Write)] Stream archiveFullBlob,
            [Blob("archive-cleaned/{DateTime.Now}.txt", FileAccess.Write)] Stream archiveCleanedBlob,
            ILogger log)
        {
            var azurecomUrl = "https://azure.microsoft.com/robotsitemap/en-us/{0}/";
            string sundogPayload = await Payload.Get("https://sundog.azure.net/api/modules?status=1", httpClient, log);

            StreamReader reader = new StreamReader(customUrlsBlob);
            var content = reader.ReadToEnd();
            var customUrls = content.Split(Environment.NewLine).Where(x => !string.IsNullOrWhiteSpace(x));

            List<string> urls = new List<string>();
            var completed = false;
            var pageNum = 1;

            while (!completed)
            {
                try
                {
                    string payload = await Payload.Get(string.Format(azurecomUrl, pageNum), httpClient, log);
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

            urls = urls.Distinct().ToList();

            var cleanedUrls = urls.Where(x =>
                !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/blog\/(.*?)\/") &&
                (
                    !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/resources\/(.*?)\/") ||
                    (
                        x.Equals("https://azure.microsoft.com/en-us/resources/knowledge-center/") ||
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
                !Regex.IsMatch(x, @"https:\/\/azure\.microsoft\.com\/en-us\/solutions\/architecture\/(.*?)\/"))
                .ToList();

            urls.AddRange(customUrls);
            cleanedUrls.AddRange(customUrls);

            urls = urls.Distinct().OrderBy(x => x).ToList();
            cleanedUrls = cleanedUrls.Distinct().OrderBy(x => x).ToList();

            log.LogInformation($"Writing to Blob Storage start.");
            latestFullBlob.Write(Encoding.Default.GetBytes(string.Join('\n', urls)));
            latestCleanedBlob.Write(Encoding.Default.GetBytes(string.Join('\n', cleanedUrls)));
            latestModulesBlob.Write(Encoding.Default.GetBytes(sundogPayload));
            archiveFullBlob.Write(Encoding.Default.GetBytes(string.Join('\n', urls)));
            archiveCleanedBlob.Write(Encoding.Default.GetBytes(string.Join('\n', cleanedUrls)));
            log.LogInformation($"Writing to Blob Storage complete.");

            log.LogInformation($"Queueing messages start.");
            Parallel.ForEach(cleanedUrls, async x =>
            {
                await msg.AddAsync(x);
            });
            log.LogInformation($"Queueing messages complete.");
        }
    }
}
