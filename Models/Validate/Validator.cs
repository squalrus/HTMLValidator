using HtmlAgilityPack;
using HTMLValidator.Extensions;
using Json.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HTMLValidator.Models.Validate
{
    public class Validator
    {
        private string _html;
        private ModuleSchema[] _schema;
        private HttpClient _client;
        private ILogger _logger;
        public Validator(string html, ModuleSchema[] schema, HttpClient client, ILogger logger)
        {
            _html = html;
            _schema = schema;
            _client = client;
            _logger = logger;
        }

        public async Task<ReportPage> Process()
        {
            var report = new ReportPage
            {
                Modules = new List<ReportModule>(),
                Classes = new Dictionary<string, int>(),
            };

            var validNodes = 0;
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(_html);

            var moduleNodes = htmlDoc.DocumentNode
                   .SelectNodes("//main//*[contains(concat(' ', @class, ' '), ' section ')]")
                   ?.Select(x => x.ToSchemaNode());

            var classNodes = htmlDoc.DocumentNode
                .SelectNodes("//main//*[@class]");

            // Parse classes
            if (classNodes != null)
            {
                foreach (var node in classNodes)
                {
                    var classes = node.Attributes["class"].Value.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x));
                    foreach (var cssClass in classes)
                    {
                        if (report.Classes.ContainsKey(cssClass))
                        {
                            report.Classes[cssClass] += 1;
                        }
                        else
                        {
                            report.Classes.Add(cssClass, 1);
                        }
                    }
                }
            }


            // Parse modules
            if (moduleNodes != null)
            {
                foreach (var node in moduleNodes)
                {
                    var nodeJson = JsonSerializer.Serialize(node, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    if (!node.Attributes.ContainsKey("data-module"))
                    {
                        report.Modules.Add(new ReportModule("undefined", "Missing data-module"));
                    }
                    else
                    {
                        var module = node.Attributes.Where(x => x.Key == "data-module").First().Value.First();
                        var schemaJson = _schema.FirstOrDefault(x => x.Slug == module)?.Schema?.ToString();

                        if (schemaJson == null)
                        {
                            report.Modules.Add(new ReportModule(module, "schema not defined"));
                        }
                        else
                        {
                            var json = JsonDocument.Parse(nodeJson);
                            var schema = JsonSchema.FromText(schemaJson);

                            string pattern = @"\$ref\"":\s*\""(.*?)\""";
                            Match m = Regex.Match(schemaJson, pattern);
                            while (m.Success)
                            {
                                var url = m.Groups[1].Value;
                                string subSchema = await Payload.Get(url, _client, _logger);
                                SchemaRegistry.Global.Register(new Uri(url), JsonSchema.FromText(subSchema));
                                m = m.NextMatch();
                            }

                            var results = schema.Validate(json.RootElement);

                            if (results.IsValid)
                            {
                                report.Modules.Add(new ReportModule(module, "pass"));
                                validNodes++;
                            }
                            else
                            {
                                report.Modules.Add(new ReportModule(module, "fail", string.Join("\n", results.Message)));
                            }
                        }
                    }
                }

                report.Total = (double)validNodes / moduleNodes.Count();
            }
            else
            {
                Console.WriteLine("URL must be on https://azure.microsoft.com with \"section\" classes present.");
            }

            return report;
        }
    }
}
