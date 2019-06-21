using HtmlAgilityPack;
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
    class Program
    {
        static void Main(string[] args)
        {
            var baseUrl = "https://azure.microsoft.com/en-us/services/azure-bastion/";

            JSchema schema = JSchema.Parse(File.ReadAllText("../../../hero-basic-schema.json"));

            try
            {
                WebRequest request = WebRequest.Create(baseUrl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string payload = reader.ReadToEnd();
                // Console.WriteLine($"Payoad {payload}");

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(payload);
                var complete = false;

                var nodes = htmlDoc.DocumentNode
                    .SelectNodes("//main/*[contains(@class, 'section')]")
                    .Select(x => x.ToNewNode());

                foreach (var node in nodes)
                {
                    if (!complete)
                    {
                        var json = JsonConvert.SerializeObject(node,
                            Formatting.Indented,
                            new JsonSerializerSettings()
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            });

                        // Console.WriteLine(json);
                        Console.WriteLine(JObject.Parse(json).IsValid(schema));
                    }

                    // complete = true;
                }

                response.Close();
            }
            catch (WebException e)
            {
                Console.WriteLine(e);
            }

        }
    }

    public class NewNode
    {
        public string Element { get; set; }
        public string[] Classes { get; set; }
        // public Dictionary<string, string[]> Attributes { get; set; }
        public NewNode[] ChildNodes { get; set; }
    }

    public static class Extensions
    {
        public static NewNode ToNewNode(this HtmlNode node)
        {
            var classAttributes = node.Attributes.Where(x => x.Name == "class");

            if (node.Name == "#text" || node.Name == "svg")
            {
                return null;
            }

            var newNode = new NewNode
            {
                Element = node.Name,
                // Attributes = node.Attributes.ToDictionary(x => x.Name, x => x.Value.Split(' ')),
                ChildNodes = node.ChildNodes.ToNewNodes(),
            };

            if (classAttributes.Any() != false) {
                newNode.Classes = classAttributes.First().Value.Split(' ');
            }

            return newNode;
        }

        public static NewNode[] ToNewNodes(this HtmlNodeCollection nodes)
        {
            var newNodes = new List<NewNode>();

            foreach (var node in nodes)
            {
                newNodes.Add(node.ToNewNode());
            }

            return newNodes.Where(x => x != null).ToArray();
        }
    }
}
