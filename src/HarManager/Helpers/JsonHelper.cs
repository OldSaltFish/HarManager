using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using HarManager.ViewModels;

namespace HarManager.Helpers
{
    public static class JsonHelper
    {
        public static List<JsonTreeNode> ParseJson(string json)
        {
            var nodes = new List<JsonTreeNode>();
            if (string.IsNullOrWhiteSpace(json)) return nodes;

            try
            {
                var token = JToken.Parse(json);
                var root = new JsonTreeNode("Root", token);

                // User requested to hide the root node if possible.
                // If it's a container (Object/Array), return its children directly.
                if (token.Type == JTokenType.Object || token.Type == JTokenType.Array)
                {
                    foreach (var child in root.Children)
                    {
                        nodes.Add(child);
                    }
                }
                else
                {
                    // For primitive values, we must show the root node to show the value.
                    nodes.Add(root);
                }
            }
            catch
            {
                // Return empty if invalid JSON
            }
            return nodes;
        }
    }
}
