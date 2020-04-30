using HtmlAgilityPack;
using HTMLValidator.Models.Validate;
using System.Collections.Generic;
using System.Linq;

namespace HTMLValidator.Extensions
{
    public static class SchemaNodeExtensions
    {
        public static SchemaNode ToSchemaNode(this HtmlNode node)
        {
            var classAttributes = node.Attributes.Where(x => x.Name == "class");
            var allAttributes = new Dictionary<string, string[]>();

            if (node.Name == "#text" || node.Name == "svg")
            {
                return null;
            }

            var newNode = new SchemaNode
            {
                Element = node.Name,
                ChildNodes = node.ChildNodes.ToSchemaNodes(),
            };

            foreach (var attribute in node.Attributes)
            {
                if (!allAttributes.Keys.Contains(attribute.Name))
                {
                    allAttributes.Add(attribute.Name, attribute.Value.Split(' '));
                }
            }

            newNode.Attributes = allAttributes;

            if (classAttributes.Any() != false)
            {
                newNode.Classes = classAttributes.First().Value.Split(' ');
            }

            return newNode;
        }

        public static SchemaNode[] ToSchemaNodes(this HtmlNodeCollection nodes)
        {
            var newNodes = new List<SchemaNode>();

            foreach (var node in nodes)
            {
                newNodes.Add(node.ToSchemaNode());
            }

            return newNodes.Where(x => x != null).ToArray();
        }

    }
}
