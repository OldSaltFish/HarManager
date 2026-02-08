using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HarManager.Models;

namespace HarManager.ViewModels
{
    public partial class ProjectViewModel : ObservableObject
    {
        private readonly Project _project;

        public ProjectViewModel(Project project)
        {
            _project = project;
            Entries = new ObservableCollection<HarEntry>(_project.Entries);
        }

        public string Name
        {
            get => _project.Name;
            set
            {
                if (_project.Name != value)
                {
                    _project.Name = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int Id => _project.Id;

        public ObservableCollection<HarEntry> Entries { get; }
        
        public Project GetEntity() => _project;
    }
}

