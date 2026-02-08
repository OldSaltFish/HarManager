using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace HarManager.Models
{
    public class HarRoot
    {
        [JsonProperty("log")]
        public HarLog Log { get; set; } = new();
    }

    public class HarLog
    {
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("creator")]
        public HarCreator Creator { get; set; } = new();

        [JsonProperty("pages")]
        public List<HarPage> Pages { get; set; } = new();

        [JsonProperty("entries")]
        public List<HarEntry> Entries { get; set; } = new();
    }

    public class HarCreator
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;
    }

    public class HarPage
    {
        [JsonProperty("startedDateTime")]
        public DateTime StartedDateTime { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;
    }

    public class HarEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public int Id { get; set; }

        [JsonIgnore]
        public int ProjectId { get; set; }
        
        [JsonIgnore]
        public Guid Uid { get; set; } = Guid.NewGuid();

        [JsonIgnore]
        public virtual Project? Project { get; set; }

        [JsonIgnore]
        public int SortOrder { get; set; }

        [JsonIgnore]
        public DateTime LastModified { get; set; } = DateTime.Now;

        [JsonIgnore]
        public string SourceFile { get; set; } = "Imported";

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("startedDateTime")]
        public DateTime StartedDateTime { get; set; }

        [JsonProperty("time")]
        public double Time { get; set; }

        [JsonProperty("request")]
        public HarRequest Request { get; set; } = new();

        [JsonProperty("response")]
        public HarResponse Response { get; set; } = new();

        [JsonProperty("cache")]
        [NotMapped]
        public object? Cache { get; set; }

        [JsonProperty("timings")]
        [NotMapped]
        public object? Timings { get; set; }

        [JsonProperty("serverIPAddress")]
        public string? ServerIPAddress { get; set; }

        [JsonProperty("connection")]
        public string? Connection { get; set; }
    }

    public class HarRequest
    {
        [JsonProperty("method")]
        public string Method { get; set; } = string.Empty;

        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        [JsonProperty("httpVersion")]
        public string HttpVersion { get; set; } = string.Empty;

        [JsonProperty("cookies")]
        public List<HarCookie> Cookies { get; set; } = new();

        [JsonProperty("headers")]
        public List<HarHeader> Headers { get; set; } = new();

        [JsonProperty("queryString")]
        public List<HarQueryString> QueryString { get; set; } = new();

        [JsonProperty("postData")]
        public HarPostData? PostData { get; set; }

        [JsonProperty("headersSize")]
        public int HeadersSize { get; set; }

        [JsonProperty("bodySize")]
        public int BodySize { get; set; }
    }

    public class HarResponse
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("statusText")]
        public string StatusText { get; set; } = string.Empty;

        [JsonProperty("httpVersion")]
        public string HttpVersion { get; set; } = string.Empty;

        [JsonProperty("cookies")]
        public List<HarCookie> Cookies { get; set; } = new();

        [JsonProperty("headers")]
        public List<HarHeader> Headers { get; set; } = new();

        [JsonProperty("content")]
        public HarContent Content { get; set; } = new();

        [JsonProperty("redirectURL")]
        public string RedirectURL { get; set; } = string.Empty;

        [JsonProperty("headersSize")]
        public int HeadersSize { get; set; }

        [JsonProperty("bodySize")]
        public int BodySize { get; set; }
    }

    public class HarCookie
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;

        [JsonProperty("path")]
        public string? Path { get; set; }

        [JsonProperty("domain")]
        public string? Domain { get; set; }

        [JsonProperty("expires")]
        public DateTime? Expires { get; set; }

        [JsonProperty("httpOnly")]
        public bool HttpOnly { get; set; }

        [JsonProperty("secure")]
        public bool Secure { get; set; }
    }

    public class HarHeader
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;
    }

    public class HarQueryString
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;
    }

    public class HarPostData
    {
        [JsonProperty("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("params")]
        public List<HarPostDataParam>? Params { get; set; }
    }

    public class HarPostDataParam
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("fileName")]
        public string? FileName { get; set; }

        [JsonProperty("contentType")]
        public string? ContentType { get; set; }
    }

    public class HarContent
    {
        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonProperty("text")]
        public string? Text { get; set; }
        
        [JsonProperty("encoding")]
        public string? Encoding { get; set; }
    }
}

