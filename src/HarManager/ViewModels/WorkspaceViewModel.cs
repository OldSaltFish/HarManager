#define DEBUG
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarManager.Data;
using HarManager.Helpers;
using HarManager.Models;
using HarManager.Services;
using HarManager.Services.Sync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HarManager.ViewModels;

public partial class WorkspaceViewModel : ViewModelBase
{
    private readonly ProjectViewModel _project;
    private readonly AppDbContext _dbContext;
    private readonly MainWindowViewModel _mainViewModel;
    private readonly HarService _harService;

    [ObservableProperty]
    private ObservableCollection<HarEntry> _filteredEntries = new ObservableCollection<HarEntry>();

    [ObservableProperty]
    private ObservableCollection<GroupItem> _groups = new ObservableCollection<GroupItem>();

    [ObservableProperty]
    private GroupItem? _selectedGroup;

    [ObservableProperty]
    private IList? _selectedEntries;

    [ObservableProperty]
    private HarEntry? _selectedEntry;

    [ObservableProperty]
    private TextDocument _requestDocument = new TextDocument();

    [ObservableProperty]
    private string _requestLanguage = "json";

    [ObservableProperty]
    private TextDocument _responseDocument = new TextDocument();

    [ObservableProperty]
    private string _responseLanguage = "json";

    [ObservableProperty]
    private string _requestBodyViewMode = "Raw";

    [ObservableProperty]
    private string _responseBodyViewMode = "Raw";

    [ObservableProperty]
    private bool _hasRequestBody;

    [ObservableProperty]
    private bool _hasQueryParams;

    [ObservableProperty]
    private bool _isRequestJson;

    [ObservableProperty]
    private bool _isRequestForm;

    [ObservableProperty]
    private string _tableModeLabel = "Table";

    [ObservableProperty]
    private bool _isResponseJson;

    [ObservableProperty]
    private string _selectedFilter = "XHR/Fetch";

    [ObservableProperty]
    private bool _isWordWrapEnabled = true;

    public ProjectViewModel Project => _project;

    public ObservableCollection<string> Filters { get; } = new ObservableCollection<string> { "XHR/Fetch", "Doc", "JS", "CSS", "Img", "Media", "Font", "Other", "All" };

    public WorkspaceViewModel(ProjectViewModel project, AppDbContext dbContext, MainWindowViewModel mainViewModel)
    {
        _project = project;
        _dbContext = dbContext;
        _mainViewModel = mainViewModel;
        _harService = new HarService();
        Project.Entries.CollectionChanged += OnEntriesCollectionChanged;
        UpdateGroups();
        UpdateFilteredEntries();
    }

    private void UpdateRequestBodyContent()
    {
        if (SelectedEntry?.Request.PostData == null)
        {
            RequestDocument.Text = "";
            return;
        }
        string text = SelectedEntry.Request.PostData.Text ?? "";
        string mimeType = SelectedEntry.Request.PostData.MimeType ?? "";
        if (RequestBodyViewMode == "JSON")
        {
            RequestDocument.Text = FormatService.FormatContent(text, mimeType);
        }
        else
        {
            RequestDocument.Text = text;
        }
    }

    private void UpdateResponseBodyContent()
    {
        if (SelectedEntry?.Response.Content == null)
        {
            ResponseDocument.Text = "";
            return;
        }
        string text = SelectedEntry.Response.Content.Text ?? "";
        string mimeType = SelectedEntry.Response.Content.MimeType ?? "";
        if (ResponseBodyViewMode == "JSON")
        {
            ResponseDocument.Text = FormatService.FormatContent(text, mimeType);
        }
        else
        {
            ResponseDocument.Text = text;
        }
    }

    private void UpdateGroups()
    {
        HashSet<string> hashSet = Groups.Select((GroupItem g) => g.Name).ToHashSet();
        List<string> list = (from f in Project.Entries.Select((HarEntry e) => e.SourceFile).Distinct()
            orderby f
            select f).ToList();
        foreach (string item in list)
        {
            if (!hashSet.Contains(item))
            {
                Groups.Add(new GroupItem
                {
                    Name = item,
                    OriginalName = item
                });
            }
        }
    }

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateGroups();
        UpdateFilteredEntries();
    }

    private void UpdateFilteredEntries()
    {
        IEnumerable<HarEntry> collection = Project.Entries.Where(IsMatch);
        FilteredEntries = new ObservableCollection<HarEntry>(collection);
    }

    private bool IsMatch(HarEntry entry)
    {
        if (SelectedGroup != null && entry.SourceFile != SelectedGroup.Name)
        {
            return false;
        }
        if (SelectedFilter == "All")
        {
            return true;
        }
        string text = entry.Response.Content.MimeType?.ToLowerInvariant() ?? "";
        return SelectedFilter switch
        {
            "XHR/Fetch" => text.Contains("json") || text.Contains("xml"), 
            "JS" => text.Contains("javascript") || text.Contains("ecmascript"), 
            "CSS" => text.Contains("css"), 
            "Img" => text.StartsWith("image/"), 
            "Media" => text.StartsWith("audio/") || text.StartsWith("video/"), 
            "Font" => text.Contains("font"), 
            "Doc" => text.Contains("html"), 
            "Other" => !text.Contains("json") && !text.Contains("xml") && !text.Contains("javascript") && !text.Contains("ecmascript") && !text.Contains("css") && !text.StartsWith("image/") && !text.StartsWith("audio/") && !text.StartsWith("video/") && !text.Contains("font") && !text.Contains("html"), 
            _ => true, 
        };
    }

    [RelayCommand]
    private void GoBack()
    {
        _mainViewModel.NavigateToProjectList();
    }

    [RelayCommand]
    private void OpenSyncSettings()
    {
        _mainViewModel.NavigateToSyncSettings();
    }

    [RelayCommand]
    private async Task ImportHar(IStorageProvider storageProvider)
    {
        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open HAR File",
            AllowMultiple = true,
            FileTypeFilter = new FilePickerFileType[1]
            {
                new FilePickerFileType("HAR Files")
                {
                    Patterns = new string[1] { "*.har" }
                }
            }
        });
        if (files.Count > 0)
        {
            await LoadHarFilesAsync(files.Select((IStorageFile f) => f.Path.LocalPath));
        }
    }

    public async Task LoadHarFilesAsync(IEnumerable<string> filePaths)
    {
        foreach (string path in filePaths)
        {
            string fileName = Path.GetFileName(path);
            HarRoot? harRoot = await _harService.ParseHarFileAsync(path);
            if (harRoot?.Log?.Entries == null)
            {
                continue;
            }
            int currentMaxSort = (Project.Entries.Any() ? Project.Entries.Max((HarEntry e) => e.SortOrder) : 0);
            int i = 1;
            foreach (HarEntry entry in harRoot.Log.Entries)
            {
                entry.SortOrder = currentMaxSort + i++;
                entry.SourceFile = fileName;
                Project.Entries.Add(entry);
                Project.GetEntity().Entries.Add(entry);
            }
        }
        _dbContext.SaveChanges();
        UpdateGroups();
        UpdateFilteredEntries();
    }

    [RelayCommand]
    private void DeleteEntries(IList? items)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }
        List<HarEntry> list = items.Cast<HarEntry>().ToList();
        foreach (HarEntry item in list)
        {
            Project.Entries.Remove(item);
            Project.GetEntity().Entries.Remove(item);
            _dbContext.HarEntries.Remove(item);
        }
        _dbContext.SaveChanges();
        UpdateGroups();
        UpdateFilteredEntries();
    }

    [RelayCommand]
    private void KeepOnlySelectedEntries()
    {
        if (SelectedEntries == null || SelectedEntries.Count == 0)
        {
            return;
        }
        List<HarEntry> second = SelectedEntries.Cast<HarEntry>().ToList();
        List<HarEntry> list = Project.Entries.Except(second).ToList();
        foreach (HarEntry item in list)
        {
            Project.Entries.Remove(item);
            Project.GetEntity().Entries.Remove(item);
            _dbContext.HarEntries.Remove(item);
        }
        _dbContext.SaveChanges();
        UpdateGroups();
        UpdateFilteredEntries();
    }

    [RelayCommand]
    private async Task CreateGroup()
    {
        await Task.Yield(); // Make it truly async to satisfy compiler warning
        
        // Prompt for name
        // Since we don't have a dialog service easily available in this context without extra code,
        // We'll use a temporary name and enter rename mode immediately, 
        // OR we can try to show a simple input dialog if we had one.
        // Given constraints, "Enter rename mode immediately" is a good UX pattern.
        
        int num = 1;
        string newName = $"New Group";
        while (Groups.Any((GroupItem g) => g.Name == newName))
        {
            newName = $"New Group {num++}";
        }

        GroupItem groupItem = new GroupItem
        {
            Name = newName,
            OriginalName = newName,
            IsRenaming = true // Auto-enter rename mode
        };
        Groups.Add(groupItem);
        SelectedGroup = groupItem;
        
        // Focus logic would be in View code-behind or behavior, but IsRenaming trigger should show the TextBox
    }

    [RelayCommand]
    private void DeleteGroup(GroupItem group)
    {
        if (group == null)
        {
            return;
        }
        List<HarEntry> list = Project.Entries.Where((HarEntry e) => e.SourceFile == group.Name).ToList();
        foreach (HarEntry item in list)
        {
            Project.Entries.Remove(item);
            Project.GetEntity().Entries.Remove(item);
            _dbContext.HarEntries.Remove(item);
        }
        Groups.Remove(group);
        if (SelectedGroup == group)
        {
            SelectedGroup = Groups.FirstOrDefault();
        }
        _dbContext.SaveChanges();
        UpdateFilteredEntries();
    }

    [RelayCommand]
    private void MoveEntriesToGroup(GroupItem targetGroup)
    {
        if (SelectedEntries == null || SelectedEntries.Count == 0 || targetGroup == null)
        {
            return;
        }
        foreach (HarEntry selectedEntry in SelectedEntries)
        {
            selectedEntry.SourceFile = targetGroup.Name;
        }
        _dbContext.SaveChanges();
        UpdateGroups();
        UpdateFilteredEntries();
    }

    [RelayCommand]
    private void MoveEntriesToNewGroup()
    {
        if (SelectedEntries == null || SelectedEntries.Count == 0)
        {
            return;
        }
        int num = 1;
        string newName = $"Group {num}";
        while (Groups.Any((GroupItem g) => g.Name == newName))
        {
            num++;
            newName = $"Group {num}";
        }
        GroupItem groupItem = new GroupItem
        {
            Name = newName,
            OriginalName = newName
        };
        Groups.Add(groupItem);
        foreach (HarEntry selectedEntry in SelectedEntries)
        {
            selectedEntry.SourceFile = groupItem.Name;
        }
        _dbContext.SaveChanges();
        UpdateFilteredEntries();
        SelectedGroup = groupItem;
    }

    [RelayCommand]
    private void RenameGroup(GroupItem group)
    {
        group.IsRenaming = true;
    }

    [RelayCommand]
    private void ConfirmRenameGroup(GroupItem group)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
        {
            group.Name = group.OriginalName;
            group.IsRenaming = false;
            return;
        }
        if (group.Name != group.OriginalName)
        {
            List<HarEntry> list = Project.Entries.Where((HarEntry e) => e.SourceFile == group.OriginalName).ToList();
            foreach (HarEntry item in list)
            {
                item.SourceFile = group.Name;
            }
            _dbContext.SaveChanges();
            group.OriginalName = group.Name;
            UpdateGroups();
            UpdateFilteredEntries();
        }
        group.IsRenaming = false;
    }

    [RelayCommand]
    private async Task ExportGroup(GroupItem group)
    {
        IApplicationLifetime? applicationLifetime = Application.Current?.ApplicationLifetime;
        if (!(applicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) || desktop.MainWindow == null)
        {
            return;
        }
        IStorageFile? file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Group as HAR",
            DefaultExtension = "har",
            SuggestedFileName = group.Name + ".har",
            FileTypeChoices = new FilePickerFileType[1]
            {
                new FilePickerFileType("HAR Files")
                {
                    Patterns = new string[1] { "*.har" }
                }
            }
        });
        if (file == null)
        {
            return;
        }
        List<HarEntry> entries = Project.Entries.Where((HarEntry e) => e.SourceFile == group.Name).ToList();
        HarRoot harRoot = new HarRoot
        {
            Log = new HarLog
            {
                Version = "1.2",
                Creator = new HarCreator
                {
                    Name = "HarManager",
                    Version = "1.0"
                },
                Entries = entries
            }
        };
        string json = JsonConvert.SerializeObject(harRoot, Formatting.Indented);
        await using Stream stream = await file.OpenWriteAsync();
        using StreamWriter writer = new StreamWriter(stream);
        await writer.WriteAsync(json);
    }

    [RelayCommand]
    private async Task CopyAsCurl(HarEntry? entry)
    {
        if (entry == null)
        {
            return;
        }
        StringBuilder sb = new StringBuilder();
        sb.Append("curl");
        sb.Append($" -X {entry.Request.Method}");
        sb.Append($" \"{entry.Request.Url}\"");
        foreach (HarHeader header in entry.Request.Headers)
        {
            if (!header.Name.StartsWith(":"))
            {
                sb.Append($" -H \"{header.Name}: {header.Value}\"");
            }
        }
        if (entry.Request.PostData != null && !string.IsNullOrEmpty(entry.Request.PostData.Text))
        {
            string body = entry.Request.PostData.Text.Replace("\"", "\\\"");
            sb.Append($" -d \"{body}\"");
        }
        string curlCommand = sb.ToString();
        IApplicationLifetime? applicationLifetime = Application.Current?.ApplicationLifetime;
        if (applicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(curlCommand);
        }
    }

    [RelayCommand]
    private async Task CopyParamsAsJson(IList? items)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }
        Dictionary<string, string> dict = new Dictionary<string, string>();
        foreach (object item in items)
        {
            if (item is HarQueryString qs)
            {
                dict[qs.Name] = qs.Value;
            }
            else if (item is HarPostDataParam pp)
            {
                dict[pp.Name] = pp.Value ?? "";
            }
            else if (item is HarHeader h)
            {
                dict[h.Name] = h.Value;
            }
            else if (item is HarCookie c)
            {
                dict[c.Name] = c.Value;
            }
        }
        string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
        IApplicationLifetime? applicationLifetime = Application.Current?.ApplicationLifetime;
        if (applicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(json);
        }
    }

    [RelayCommand]
    private async Task CopyParamsAsColon(IList? items)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }
        StringBuilder sb = new StringBuilder();
        foreach (object item in items)
        {
            string name = "";
            string value = "";
            if (item is HarQueryString qs)
            {
                name = qs.Name;
                value = qs.Value;
            }
            else if (item is HarPostDataParam pp)
            {
                name = pp.Name;
                value = pp.Value ?? "";
            }
            else if (item is HarHeader h)
            {
                name = h.Name;
                value = h.Value;
            }
            else if (item is HarCookie c)
            {
                name = c.Name;
                value = c.Value;
            }
            if (!string.IsNullOrEmpty(name))
            {
                sb.AppendLine($"{name}:{value}");
            }
        }
        string text = sb.ToString().TrimEnd();
        IApplicationLifetime? applicationLifetime = Application.Current?.ApplicationLifetime;
        if (applicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(text);
        }
    }

    [RelayCommand]
    private async Task CopyCookiesAsSemicolon(IList? items)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }
        StringBuilder sb = new StringBuilder();
        foreach (object item in items)
        {
            if (item is HarCookie c)
            {
                sb.Append($"{c.Name}={c.Value}; ");
            }
        }
        if (sb.Length > 0)
        {
            sb.Length -= 2;
        }
        string text = sb.ToString();
        IApplicationLifetime? applicationLifetime = Application.Current?.ApplicationLifetime;
        if (applicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(text);
        }
    }

    [RelayCommand]
    private async Task CopyRequestBodyAsJson()
    {
        if (SelectedEntry?.Request.PostData == null)
        {
            return;
        }
        string textToCopy = "";
        if (RequestBodyViewMode == "Table")
        {
            if (SelectedEntry.Request.PostData.Params != null)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                foreach (HarPostDataParam p in SelectedEntry.Request.PostData.Params)
                {
                    dict[p.Name] = p.Value ?? "";
                }
                textToCopy = JsonConvert.SerializeObject(dict, Formatting.Indented);
            }
        }
        else
        {
            textToCopy = RequestDocument.Text;
        }
        IClassicDesktopStyleApplicationLifetime? desktop = null;
        int num;
        if (!string.IsNullOrEmpty(textToCopy))
        {
            IApplicationLifetime? applicationLifetime = Application.Current?.ApplicationLifetime;
            desktop = applicationLifetime as IClassicDesktopStyleApplicationLifetime;
            num = ((desktop != null) ? 1 : 0);
        }
        else
        {
            num = 0;
        }
        if (num != 0 && desktop?.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(textToCopy);
        }
    }

    [RelayCommand]
    private async Task CopyRequestBodyAsColon()
    {
        if (SelectedEntry?.Request.PostData == null)
        {
            return;
        }
        string textToCopy = "";
        if (RequestBodyViewMode == "Table")
        {
            if (SelectedEntry.Request.PostData.Params != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (HarPostDataParam p in SelectedEntry.Request.PostData.Params)
                {
                    sb.AppendLine($"{p.Name}:{p.Value ?? ""}");
                }
                textToCopy = sb.ToString().TrimEnd();
            }
        }
        else if (IsRequestJson)
        {
            try
            {
                JToken token = JToken.Parse(RequestDocument.Text);
                if (token is JObject obj)
                {
                    StringBuilder sb2 = new StringBuilder();
                    foreach (JProperty prop in obj.Properties())
                    {
                        sb2.AppendLine($"{prop.Name}:{prop.Value}");
                    }
                    textToCopy = sb2.ToString().TrimEnd();
                }
                else
                {
                    textToCopy = RequestDocument.Text;
                }
            }
            catch
            {
                textToCopy = RequestDocument.Text;
            }
        }
        else
        {
            textToCopy = RequestDocument.Text;
        }
        IApplicationLifetime? applicationLifetime = Application.Current?.ApplicationLifetime;
        IClassicDesktopStyleApplicationLifetime? desktop = null;
        desktop = applicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (desktop != null && !string.IsNullOrEmpty(textToCopy) && desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(textToCopy);
        }
    }

    [RelayCommand]
    private async Task CopyNodeValue(JsonTreeNode? node)
    {
        if (node != null)
        {
            string textToCopy = ((!(node.Token is JValue)) ? node.Token.ToString(Formatting.Indented) : node.Token.ToString());
            IApplicationLifetime? applicationLifetime = Application.Current?.ApplicationLifetime;
            if (applicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(textToCopy);
            }
        }
    }

    [RelayCommand]
    private void SaveChanges()
    {
        try
        {
            _dbContext.SaveChanges();
            // Auto-sync current entry if selected
            if (SelectedEntry != null)
            {
                _ = SyncEntry(SelectedEntry);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error saving changes: " + ex.Message);
        }
    }

    [ObservableProperty]
    private bool _isSimplifiedUrlMode;

    [ObservableProperty]
    private string _syncStatus = "Ready to sync";

    [RelayCommand]
    private async Task SyncEntry(HarEntry entry)
    {
        if (entry == null || SyncManager.Instance.CurrentProvider == null) return;
        
        try
        {
            SyncStatus = "Syncing entry...";
            
            // Re-use sync logic but only for this entry
            // Actually, we usually sync by group/file.
            // But user asked for single request granularity.
            // Let's implement single entry sync if provider supports it or we update the whole group file.
            
            // Since HAR is a list of entries, we usually sync the whole HAR file (Group).
            // Updating a single entry inside a remote HAR file is hard without downloading, parsing, updating, uploading.
            // So "Sync Single Request" implies we might want to just re-upload the Group file that contains this request.
            // Or maybe the user wants each request to be an individual file?
            // "现在同步的粒度还是太大，我说了要以单个请求为粒度" -> "Sync granularity is too big, I said single request granularity"
            
            // Interpretation: User wants to manage individual requests as individual files on the cloud?
            // OR User just wants to trigger sync when a single request is modified.
            // Let's assume the latter first: Auto-sync when a request is modified.
            // But if user wants to sync ONLY that request, maybe they mean the remote structure should be 1 file per request?
            // That would change the architecture significantly (Group = Folder, Entry = File).
            
            // Let's stick to "Trigger sync when entry changes" and "Update the group file" for now, 
            // but maybe optimize to only upload if changed.
            // Wait, "I said single request granularity" strongly suggests 1 request = 1 file.
            // If so, we need to change how we store/sync.
            
            // Current: Group = .har file.
            // New Proposal: Group = Folder, Entry = .har file (with 1 entry) or .json file.
            // Let's ask user? No, "Be extremely biased for action".
            // If I change to 1 request = 1 file, it might break existing structure.
            // But if user insists, I should try to support it or explain.
            // However, "Default to Project List" -> Project -> Group -> Entries.
            // If I make Entry = File, then Group = Folder.
            
            // Let's implement "Auto-sync" first (SyncStatus update).
            // And for granularity, let's try to upload just the changed entry if possible?
            // No, WebDav is file based.
            
            // Let's assume "Single Request Granularity" means:
            // When I modify a request, ONLY that request is synced (maybe to a specific file?).
            // Let's switch to: Group = Folder on WebDav, Entry = File on WebDav.
            // File name = Entry Name (sanitized) + ".json" or ".har".
            
            // Let's update SyncManager to support syncing individual entries.
            
            await SyncManager.Instance.SyncEntryAsync(Project.Name, entry.SourceFile, entry);
            SyncStatus = $"Synced {entry.Name} at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            SyncStatus = $"Sync failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SyncProject()
    {
        try
        {
            if (SyncManager.Instance.CurrentProvider == null)
            {
                SyncStatus = "Sync not configured.";
                return;
            }
            
            SyncStatus = "Syncing...";
            
            IApplicationLifetime? applicationLifetime = Application.Current?.ApplicationLifetime;
            if (applicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                // Optional: Show loading indicator
            }

            await SyncManager.Instance.SyncProjectGroupsAsync(Project.GetEntity());
            SyncStatus = $"Synced at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Sync error: " + ex.Message);
            SyncStatus = $"Sync failed: {ex.Message}";
        }
    }

    partial void OnSelectedGroupChanged(GroupItem? value)
    {
        UpdateFilteredEntries();
    }

    partial void OnSelectedEntryChanged(HarEntry? value)
    {
        if (value == null)
        {
            RequestDocument.Text = "";
            ResponseDocument.Text = "";
            IsRequestJson = false;
            IsResponseJson = false;
            HasRequestBody = false;
            HasQueryParams = false;
            RequestBodyViewMode = "Raw";
            ResponseBodyViewMode = "Raw";
            return;
        }
        if (value.Request.QueryString != null && value.Request.QueryString.Count > 0)
        {
            HasQueryParams = true;
        }
        else
        {
            HasQueryParams = false;
        }
        if (value.Request.PostData != null && (!string.IsNullOrEmpty(value.Request.PostData.Text) || (value.Request.PostData.Params != null && value.Request.PostData.Params.Count > 0)))
        {
            HasRequestBody = true;
            string text = value.Request.PostData.Text ?? "";
            string text2 = value.Request.PostData.MimeType ?? "";
            RequestLanguage = (text2.Contains("json") ? "json" : "text");
            if (text2.Contains("json") || text.TrimStart().StartsWith("{") || text.TrimStart().StartsWith("["))
            {
                try
                {
                    List<JsonTreeNode> list = JsonHelper.ParseJson(text);
                    IsRequestJson = true;
                    IsRequestForm = false;
                    RequestBodyViewMode = "JSON";
                }
                catch
                {
                    IsRequestJson = false;
                    IsRequestForm = false;
                    RequestBodyViewMode = "Raw";
                }
            }
            else if (value.Request.PostData.Params != null && value.Request.PostData.Params.Count > 0)
            {
                IsRequestJson = false;
                IsRequestForm = true;
                RequestBodyViewMode = "Table";
                if (text2.Contains("x-www-form-urlencoded"))
                {
                    TableModeLabel = "UrlEncode";
                }
                else if (text2.Contains("multipart/form-data"))
                {
                    TableModeLabel = "FormData";
                }
                else
                {
                    TableModeLabel = "Table";
                }
            }
            else
            {
                IsRequestJson = false;
                IsRequestForm = false;
                RequestBodyViewMode = "Raw";
            }
            UpdateRequestBodyContent();
        }
        else
        {
            HasRequestBody = false;
            HasQueryParams = false;
            RequestDocument.Text = "";
            IsRequestJson = false;
            IsRequestForm = false;
            RequestBodyViewMode = "Raw";
        }
        if (value.Response.Content != null && !string.IsNullOrEmpty(value.Response.Content.Text))
        {
            string text3 = value.Response.Content.Text;
            string text4 = value.Response.Content.MimeType ?? "";
            ResponseLanguage = (text4.Contains("json") ? "json" : "text");
            if (text4.Contains("json") || text3.TrimStart().StartsWith("{") || text3.TrimStart().StartsWith("["))
            {
                try
                {
                    List<JsonTreeNode> list2 = JsonHelper.ParseJson(text3);
                    IsResponseJson = true;
                    ResponseBodyViewMode = "JSON";
                }
                catch
                {
                    IsResponseJson = false;
                    ResponseBodyViewMode = "Raw";
                }
            }
            else
            {
                IsResponseJson = false;
                ResponseBodyViewMode = "Raw";
            }
            UpdateResponseBodyContent();
        }
        else
        {
            ResponseDocument.Text = "";
            IsResponseJson = false;
            ResponseBodyViewMode = "Raw";
        }
    }

    partial void OnRequestBodyViewModeChanged(string value)
    {
        UpdateRequestBodyContent();
    }

    partial void OnResponseBodyViewModeChanged(string value)
    {
        UpdateResponseBodyContent();
    }

    partial void OnSelectedFilterChanged(string value)
    {
        UpdateFilteredEntries();
    }
}

