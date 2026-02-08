using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebDav;

namespace HarManager.Services.Sync
{
    public class WebDavSyncProvider : ISyncProvider
    {
        public string Name => "WebDAV (Nutstore/Nextcloud)";
        private string _baseUrl = "";
        private string _username = "";
        private string _password = "";
        private WebDavClient? _client;
        private const string RootFolder = "HarManager";

        public async Task InitializeAsync(Dictionary<string, string> config)
        {
            if (config.TryGetValue("BaseUrl", out var url)) _baseUrl = url.TrimEnd('/');
            if (config.TryGetValue("Username", out var user)) _username = user;
            if (config.TryGetValue("Password", out var pass)) _password = pass;

            var clientParams = new WebDavClientParams
            {
                BaseAddress = new Uri(_baseUrl),
                Credentials = new NetworkCredential(_username, _password)
            };
            _client = new WebDavClient(clientParams);

            // Ensure root folder exists
            try
            {
                var response = await _client.Mkcol(RootFolder);
                // 201 Created or 405 Method Not Allowed (if already exists) are fine
            }
            catch
            {
                // Ignore initialization errors, they will appear during operations
            }
        }

        public async Task<List<RemoteItemInfo>> ListItemsAsync(string? parentId = null)
        {
            if (_client == null) throw new InvalidOperationException("Provider not initialized");

            var path = parentId ?? RootFolder;
            var result = await _client.Propfind(path);

            if (!result.IsSuccessful)
            {
                if (result.StatusCode == 404) return new List<RemoteItemInfo>();
                throw new Exception($"WebDAV Error: {result.StatusCode} {result.Description}");
            }

            var items = new List<RemoteItemInfo>();
            foreach (var res in result.Resources)
            {
                // Skip the folder itself
                // WebDav paths from Propfind are usually full paths.
                // We need to compare carefully.
                if (res.Uri == null) continue;
                var resUri = Uri.UnescapeDataString(res.Uri);
                var pathUri = path.TrimEnd('/');
                
                // Simple check if it's the requested folder itself
                if (resUri.TrimEnd('/').EndsWith(pathUri)) continue;

                // We want ID to be the relative path or full path that we can use later.
                // Let's use the full relative path from BaseUrl which WebDav client handles.
                // res.Uri usually includes the root of the server, e.g. /dav/HarManager/file.har
                // We can just use res.Uri as ID.
                
                items.Add(new RemoteItemInfo
                {
                    Id = res.Uri,
                    Name = Path.GetFileName(resUri.TrimEnd('/')),
                    LastModified = res.LastModifiedDate ?? DateTime.MinValue,
                    Size = res.ContentLength ?? 0,
                    IsFolder = res.IsCollection
                });
            }

            return items;
        }

        public async Task<string?> DownloadItemAsync(string remoteId)
        {
            if (_client == null) throw new InvalidOperationException("Provider not initialized");

            var response = await _client.GetRawFile(remoteId);
            if (!response.IsSuccessful) return null;

            using var reader = new StreamReader(response.Stream);
            return await reader.ReadToEndAsync();
        }

        public async Task<string> UploadItemAsync(string name, string content, string? remoteId = null, string? parentId = null)
        {
            if (_client == null) throw new InvalidOperationException("Provider not initialized");

            // Logic to handle parentId as folder path
            var folder = RootFolder;
            if (!string.IsNullOrEmpty(parentId))
            {
                // Ensure parent folders exist
                // parentId could be "Project/Group"
                var parts = parentId.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    folder = $"{folder}/{part}";
                    try 
                    {
                        await _client.Mkcol(folder); 
                    }
                    catch 
                    { 
                        // Ignore if exists
                    }
                }
            }

            var fileName = remoteId ?? name;
            var fullPath = $"{folder}/{fileName}";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var response = await _client.PutFile(fullPath, stream);

            if (!response.IsSuccessful)
            {
                throw new Exception($"WebDAV Upload Error: {response.StatusCode} {response.Description}");
            }

            return fullPath;
        }

        public async Task DeleteItemAsync(string remoteId)
        {
             if (_client == null) throw new InvalidOperationException("Provider not initialized");
             
             var response = await _client.Delete(remoteId);
             if (!response.IsSuccessful && response.StatusCode != 404)
             {
                 throw new Exception($"WebDAV Delete Error: {response.StatusCode} {response.Description}");
             }
        }
    }
}

