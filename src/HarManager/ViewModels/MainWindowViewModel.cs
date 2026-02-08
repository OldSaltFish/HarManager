using CommunityToolkit.Mvvm.ComponentModel;
using HarManager.Data;
using HarManager.Services;
using HarManager.Services.Sync;
using System.Collections.Generic;

namespace HarManager.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly AppDbContext _dbContext;

        [ObservableProperty]
        private ViewModelBase _currentPage;

        public MainWindowViewModel()
        {
            _dbContext = new AppDbContext();
            _dbContext.Database.EnsureCreated();

            InitializeSync();

            // Default to Project List
            _currentPage = new ProjectListViewModel(_dbContext, this);
        }

        private async void InitializeSync()
        {
            var settings = await SettingsService.LoadSettingsAsync();
            if (!string.IsNullOrEmpty(settings.BaseUrl))
            {
                var config = new Dictionary<string, string>();
                config["BaseUrl"] = settings.BaseUrl;
                if (settings.ProviderName == "WebDAV")
                {
                    config["Username"] = settings.Username;
                    config["Password"] = settings.Password;
                }
                else
                {
                    config["Token"] = settings.Username;
                }
                await SyncManager.Instance.InitializeProviderAsync(settings.ProviderName, config);
            }
        }

        public void NavigateToWorkspace(ProjectViewModel project)
        {
            CurrentPage = new WorkspaceViewModel(project, _dbContext, this);
        }

        public void NavigateToProjectList()
        {
            CurrentPage = new ProjectListViewModel(_dbContext, this);
        }

        public void NavigateToSyncSettings()
        {
            CurrentPage = new SyncSettingsViewModel(this);
        }
    }
}
