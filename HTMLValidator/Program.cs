using HtmlAgilityPack;
using Newtonsoft.Json;
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
            var baseUrl = "https://azure.microsoft.com/en-us/solutions/";

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
                        var json = JsonConvert.SerializeObject(node, Newtonsoft.Json.Formatting.Indented,
                            new JsonSerializerSettings()
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            });

                        Console.WriteLine(json);
                    }

                    complete = true;
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
        // public string Id { get; set; }
        public string Element { get; set; }
        // public string[] Classes { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public NewNode[] ChildNodes { get; set; }
    }

    public static class Extensions
    {
        public static NewNode ToNewNode(this HtmlNode node)
        {
            if (node.Name == "#text" || node.Name == "svg")
            {
                return null;
            }

            return new NewNode
            {
                // Id = node.Id,
                // Classes = node.GetClasses().ToArray(),
                Element = node.Name,
                Attributes = node.Attributes.ToDictionary(x => x.Name, x => x.Value),
                ChildNodes = node.ChildNodes.ToNewNodes(),
            };

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
