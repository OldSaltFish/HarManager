using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarManager.Services;
using HarManager.Services.Sync;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace HarManager.ViewModels
{
    public partial class SyncSettingsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainViewModel;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWebDav))]
        private string _selectedProviderName = "WebDAV";

        [ObservableProperty]
        private string _baseUrl = "";

        [ObservableProperty]
        private string _username = ""; // Or Token

        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private string _statusMessage = "";

        public bool IsWebDav => SelectedProviderName == "WebDAV";

        public ObservableCollection<string> ProviderNames { get; } = new ObservableCollection<string>
        {
            "WebDAV",
            "Custom Server"
        };

        public SyncSettingsViewModel(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadConfig();
        }

        private async void LoadConfig()
        {
            var settings = await SettingsService.LoadSettingsAsync();
            SelectedProviderName = settings.ProviderName;
            BaseUrl = settings.BaseUrl;
            Username = settings.Username;
            Password = settings.Password;
        }

        [RelayCommand]
        private async Task TestConnection()
        {
            StatusMessage = "Testing connection...";
            try
            {
                ISyncProvider provider;
                var config = new Dictionary<string, string>();

                if (SelectedProviderName == "WebDAV")
                {
                    provider = new WebDavSyncProvider();
                    config["BaseUrl"] = BaseUrl;
                    config["Username"] = Username;
                    config["Password"] = Password;
                }
                else
                {
                    provider = new CustomApiSyncProvider();
                    config["BaseUrl"] = BaseUrl;
                    config["Token"] = Username; // Use Username field as Token for CustomApi
                }

                await provider.InitializeAsync(config);
                await provider.ListItemsAsync(); // Try to list to verify connection
                StatusMessage = "Connection successful!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            var config = new Dictionary<string, string>();
            config["BaseUrl"] = BaseUrl;
            if (SelectedProviderName == "WebDAV")
            {
                config["Username"] = Username;
                config["Password"] = Password;
            }
            else
            {
                config["Token"] = Username;
            }

            try 
            {
                // Save to persistent storage
                var settings = new AppSettings
                {
                    ProviderName = SelectedProviderName,
                    BaseUrl = BaseUrl,
                    Username = Username,
                    Password = Password
                };
                await SettingsService.SaveSettingsAsync(settings);

                await SyncManager.Instance.InitializeProviderAsync(SelectedProviderName, config);
                StatusMessage = "Settings saved and connected.";
                
                // Return to Project List or Workspace?
                // For now, just show success. 
                // Maybe navigate back.
                _mainViewModel.NavigateToProjectList();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _mainViewModel.NavigateToProjectList();
        }
    }
}

