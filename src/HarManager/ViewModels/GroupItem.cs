using CommunityToolkit.Mvvm.ComponentModel;

namespace HarManager.ViewModels
{
    public partial class GroupItem : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _originalName = string.Empty;

        [ObservableProperty]
        private bool _isRenaming;
    }
}

