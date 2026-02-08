using System;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace HarManager.Services
{
    public static class FormatService
    {
        public static string FormatContent(string content, string? mimeType)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            mimeType = mimeType?.ToLowerInvariant() ?? "";

            // Try JSON
            if (mimeType.Contains("json") || content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
            {
                try
                {
                    var token = JToken.Parse(content);
                    return token.ToString(Newtonsoft.Json.Formatting.Indented);
                }
                catch
                {
                    // Not valid JSON, ignore
                }
            }

            // Try XML
            if (mimeType.Contains("xml") || content.TrimStart().StartsWith("<"))
            {
                try
                {
                    var doc = XDocument.Parse(content);
                    return doc.ToString();
                }
                catch
                {
                    // Not valid XML, ignore
                }
            }

            return content;
        }

        public static string DetectLanguage(string content, string? mimeType)
        {
            if (string.IsNullOrWhiteSpace(content)) return "text";
             mimeType = mimeType?.ToLowerInvariant() ?? "";

            if (mimeType.Contains("json")) return "json";
            if (mimeType.Contains("xml") || mimeType.Contains("html")) return "xml";
            
            var trimmed = content.TrimStart();
            if (trimmed.StartsWith("{") || trimmed.StartsWith("[")) return "json";
            if (trimmed.StartsWith("<")) return "xml";

            return "text";
        }
    }
}

