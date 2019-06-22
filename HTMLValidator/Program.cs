using HtmlAgilityPack;
using HTMLValidator.Extensions;
using HTMLValidator.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace HTMLValidator
{
    class Program
    {
        static void Main(string[] args)
        {
            var testUrl = "https://azure.microsoft.com/en-us/services/azure-bastion/";
            var moduleUrl = "https://azurecomstats.blob.core.windows.net/temp/modules.json";

            Schema[] schemaJson = null;

            try
            {
                WebRequest request = WebRequest.Create(moduleUrl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string payload = reader.ReadToEnd();

                schemaJson = JsonConvert.DeserializeObject<Schema[]>(payload);

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

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(payload);

                var nodes = htmlDoc.DocumentNode
                    .SelectNodes("//main/*[contains(@class, 'section')]")
                    .Select(x => x.ToNewNode());

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
                    }
                    else
                    {
                        var module = node.Attributes.Where(x => x.Key == "data-module").First().Value.First();
                        var schema = schemaJson.First(x => x.Slug == module)?.JsonSchema?.ToString();

                        if (schema == null)
                        {
                            Console.WriteLine($"Unable to find schema for {module}");
                        }
                        else
                        {
                            Console.WriteLine(JObject.Parse(json).IsValid(JSchema.Parse(schema)));
                        }
                    }
                }

                response.Close();
            }
            catch (WebException e)
            {
                Console.WriteLine(e);
            }

        }
    }
}
