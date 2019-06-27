using HtmlAgilityPack;
using HTMLValidator.Models;
using System.Collections.Generic;
using System.Linq;

namespace HTMLValidator.Extensions
{
    public static class NewNodeExtensions
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
                ChildNodes = node.ChildNodes.ToNewNodes(),
                Attributes = node.Attributes.ToDictionary(x => x.Name, x => x.Value.Split(' ')),
            };

            if (classAttributes.Any() != false)
            {
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
