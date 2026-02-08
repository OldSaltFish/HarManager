using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HarManager.Services.Sync
{
    public interface ISyncProvider
    {
        string Name { get; }
        
        // Initialize with configuration (e.g. BaseUrl, Token)
        Task InitializeAsync(Dictionary<string, string> config);

        // List available items (groups/entries) in the cloud
        Task<List<RemoteItemInfo>> ListItemsAsync(string? parentId = null);

        // Download an item content (JSON string)
        Task<string?> DownloadItemAsync(string remoteId);

        // Upload an item content
        // Returns the remote ID
        Task<string> UploadItemAsync(string name, string content, string? remoteId = null, string? parentId = null);
        
        // Delete an item
        Task DeleteItemAsync(string remoteId);
    }

    public class RemoteItemInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
    }
}

