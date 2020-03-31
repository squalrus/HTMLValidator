using HtmlAgilityPack;
using HTMLValidator.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HTMLValidator.Models.Validate
{
    public class Validator
    {
        private string _html;
        private ModuleSchema[] _schema;
        public Validator(string html, ModuleSchema[] schema)
        {
            _html = html;
            _schema = schema;
        }

        public ReportPage Process()
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
                    var json = JsonConvert.SerializeObject(node,
                        Formatting.Indented,
                        new JsonSerializerSettings()
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        });

                    if (!node.Attributes.ContainsKey("data-module"))
                    {
                        report.Modules.Add(new ReportModule("undefined", "Missing data-module"));
                    }
                    else
                    {
                        var module = node.Attributes.Where(x => x.Key == "data-module").First().Value.First();
                        var moduleSchema = _schema.FirstOrDefault(x => x.Slug == module)?.Schema?.ToString();
                        JSchemaUrlResolver resolver = new JSchemaUrlResolver();
                        IList<string> messages;

                        if (moduleSchema == null)
                        {
                            report.Modules.Add(new ReportModule(module, "schema not defined"));
                        }
                        else
                        {
                            var isValid = JObject.Parse(json).IsValid(JSchema.Parse(moduleSchema, resolver), out messages);

                            if (isValid)
                            {
                                report.Modules.Add(new ReportModule(module, "pass"));
                                validNodes++;
                            }
                            else
                            {
                                report.Modules.Add(new ReportModule(module, "fail", string.Join("\n", messages)));
                            }
                        }
                    }
                }

                report.Total = Math.Truncate((decimal)validNodes / moduleNodes.Count() * 100);
            }
            else
            {
                Console.WriteLine("URL must be on https://azure.microsoft.com with \"section\" classes present.");
            }

            return report;
        }
    }
}
