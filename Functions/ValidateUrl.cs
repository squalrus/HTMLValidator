using HtmlAgilityPack;
using HTMLValidator.Extensions;
using HTMLValidator.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HTMLValidator
{
    public static class ValidateUrl
    {
        [FunctionName("ValidateUrl")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing validation.");

            string testUrl = req.Query["url"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            testUrl = testUrl ?? data?.testUrl;
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
                            Console.WriteLine("Missing data-module");
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
                                Console.WriteLine($"{module}: unable to find schema");
                                output.Add($"{module}: unable to find schema");
                            }
                            else
                            {
                                var isValid = JObject.Parse(json).IsValid(JSchema.Parse(schema, resolver), out messages);

                                if (isValid)
                                {
                                    Console.WriteLine($"{module}: {isValid}");
                                    output.Add($"{module}: {isValid}");
                                    validNodes++;
                                }
                                else
                                {
                                    Console.WriteLine($"{module}: {isValid}");
                                    output.Add($"{module}: {isValid}");
                                    foreach (var message in messages)
                                    {
                                        Console.WriteLine($"\t{message}");
                                    output.Add($"\t{message}");
                                    }
                                }
                            }
                        }
                    }
                    Console.WriteLine($"\n{testUrl}: {Math.Truncate((decimal)validNodes / nodes.Count() * 100)}%");
                    output.Add($"\n{testUrl}: {Math.Truncate((decimal)validNodes / nodes.Count() * 100)}%");
                }
                else
                {
                    output.Add("URL must be on https://azure.microsoft.com with \"section\" classes present.");
                }

                response.Close();
            }
            catch (WebException e)
            {
                Console.WriteLine(e);
            }


            return testUrl != null
                ? (ActionResult)new OkObjectResult($"{string.Join('\n', output)}")
                : new BadRequestObjectResult("Please pass a url on the query string or in the request body");
        }
    }
}
