using HtmlAgilityPack;
using HTMLValidator.Extensions;
using HTMLValidator.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace HTMLValidator
{
    public static class ValidateUrlMessage
    {
        [FunctionName("ValidateUrlMessage")]
        [return: Table("coverage")]
        public static Coverage Run(
            [QueueTrigger("urls", Connection = "AzureWebJobsStorage")] string myQueueItem,
            ILogger log)
        {
            log.LogInformation("Processing validation.");

            string testUrl = myQueueItem;
            string partitionKey = testUrl.ToSlug();
            List<string> output = new List<string>();

            var moduleUrl = "https://sundog.azure.net/api/modules?status=1";

            ModuleSchema[] schemaJson = null;

            try
            {
                WebRequest request = WebRequest.Create(moduleUrl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string payload = reader.ReadToEnd();

                schemaJson = JsonConvert.DeserializeObject<ModuleSchema[]>(payload);

                response.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                WebRequest request = WebRequest.Create(testUrl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string payload = reader.ReadToEnd();
                var validNodes = 0;

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(payload);

                var nodes = htmlDoc.DocumentNode
                    .SelectNodes("//main//*[contains(concat(' ', @class, ' '), ' section ')]")
                    ?.Select(x => x.ToSchemaNode());

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        var json = JsonConvert.SerializeObject(node,
                            Formatting.Indented,
                            new JsonSerializerSettings()
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            });

                        if (!node.Attributes.ContainsKey("data-module"))
                        {
                            log.LogInformation("Missing data-module");
                            output.Add("Missing data-module");
                        }
                        else
                        {
                            var module = node.Attributes.Where(x => x.Key == "data-module").First().Value.First();
                            var schema = schemaJson.FirstOrDefault(x => x.Slug == module)?.Schema?.ToString();
                            JSchemaUrlResolver resolver = new JSchemaUrlResolver();
                            IList<string> messages;

                            if (schema == null)
                            {
                                log.LogInformation($"{module}: unable to find schema");
                                output.Add($"{module}: unable to find schema");
                            }
                            else
                            {
                                var isValid = JObject.Parse(json).IsValid(JSchema.Parse(schema, resolver), out messages);

                                if (isValid)
                                {
                                    log.LogInformation($"{module}: {isValid}");
                                    output.Add($"{module}: {isValid}");
                                    validNodes++;
                                }
                                else
                                {
                                    log.LogInformation($"{module}: {isValid}");
                                    output.Add($"{module}: {isValid}");
                                    foreach (var message in messages)
                                    {
                                        log.LogInformation($"\t{message}");
                                        output.Add($"\t{message}");
                                    }
                                }
                            }
                        }
                    }
                    log.LogInformation($"\n{testUrl}: {Math.Truncate((double)validNodes / nodes.Count() * 100)}%");
                    output.Add($"\n{testUrl}: {Math.Truncate((double)validNodes / nodes.Count() * 100)}%");
                }
                else
                {
                    output.Add("URL must be on https://azure.microsoft.com with \"section\" classes present.");
                }

                response.Close();

                return new Coverage
                {
                    PartitionKey = partitionKey,
                    RowKey = (DateTime.MaxValue.Ticks - DateTime.Now.Ticks).ToString(),
                    Report = string.Join('\n', output),
                    Percent = (double)validNodes / nodes.Count()
                };
            }
            catch (WebException e)
            {
                Console.WriteLine(e);
            }

            return new Coverage
            {
                PartitionKey = partitionKey,
                RowKey = (DateTime.MaxValue.Ticks - DateTime.Now.Ticks).ToString(),
                Report = "Parsing failure",
                Percent = 0
            };
        }
    }
}
