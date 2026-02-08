using HarManager.Data;
using HarManager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HarManager.Services.Sync
{
    public class SyncManager
    {
        public static SyncManager Instance { get; } = new SyncManager();
        
        public ISyncProvider? CurrentProvider { get; private set; }

        public void Configure(ISyncProvider provider, Dictionary<string, string> config)
        {
            CurrentProvider = provider;
            // Initialize is async, but we might need to await it elsewhere or fire and forget here (not ideal)
            // Better to have InitializeAsync
        }

        public async Task InitializeProviderAsync(string providerName, Dictionary<string, string> config)
        {
            ISyncProvider? provider = providerName switch
            {
                "WebDAV" => new WebDavSyncProvider(),
                "Custom Server" => new CustomApiSyncProvider(),
                _ => null
            };

            if (provider != null)
            {
                await provider.InitializeAsync(config);
                CurrentProvider = provider;
            }
        }

        public async Task SyncProjectGroupsAsync(Project project)
        {
            if (CurrentProvider == null) throw new InvalidOperationException("Sync provider not configured");

            // 1. Group entries by SourceFile (Group Name)
            var groups = project.Entries
                .GroupBy(e => e.SourceFile)
                .Where(g => g.Key != "Imported" && !string.IsNullOrEmpty(g.Key))
                .ToList();

            foreach (var group in groups)
            {
                var groupName = group.Key;
                
                // Ensure group folder exists (Project/Group)
                // We'll upload individual entries into this folder
                
                foreach (var entry in group)
                {
                    await SyncEntryAsync(project.Name, groupName, entry);
                }
            }
        }

        public async Task SyncEntryAsync(string projectName, string groupName, HarEntry entry)
        {
            if (CurrentProvider == null) return;

            // Structure: Project/Group/EntryName.har
            // Sanitize names
            var safeProject = SanitizeFileName(projectName);
            var safeGroup = SanitizeFileName(groupName);
            var safeEntry = SanitizeFileName(entry.Name);
            if (string.IsNullOrEmpty(safeEntry)) safeEntry = $"Entry_{entry.Id}";

            // We need to create the hierarchy. 
            // WebDav provider might need to create folders recursively.
            // Let's assume provider handles path like "Project/Group/Entry.har" 
            // or we need to implement "EnsureDirectory" in provider.
            
            // For now, let's just use a path string and hope provider handles it or we update provider.
            // Ideally: UploadItemAsync takes a path.
            
            // Construct a single-entry HAR
            var harLog = new HarLog
            {
                Version = "1.2",
                Creator = new HarCreator { Name = "HarManager", Version = "1.0" },
                Entries = new List<HarEntry> { entry }
            };
            var harRoot = new HarRoot { Log = harLog };
            var json = JsonConvert.SerializeObject(harRoot, Formatting.Indented);

            // Upload
            // We pass "Project/Group" as parent path logic
            // But UploadItemAsync signature is (name, content, remoteId, parentId).
            // Let's use parentId as the full folder path for WebDav.
            var parentPath = $"{safeProject}/{safeGroup}";
            
            await CurrentProvider.UploadItemAsync(
                name: $"{safeEntry}.har",
                content: json,
                parentId: parentPath 
            );
        }

        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }
    }
}

