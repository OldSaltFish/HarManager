using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarManager.Data;
using HarManager.Models;
using Microsoft.EntityFrameworkCore;

namespace HarManager.ViewModels
{
    public partial class ProjectListViewModel : ViewModelBase
    {
        private readonly AppDbContext _dbContext;
        private readonly MainWindowViewModel _mainViewModel;

        [ObservableProperty]
        private ObservableCollection<ProjectViewModel> _projects = new();

        [ObservableProperty]
        private ProjectViewModel? _selectedProject;

        public ProjectListViewModel(AppDbContext dbContext, MainWindowViewModel mainViewModel)
        {
            _dbContext = dbContext;
            _mainViewModel = mainViewModel;
            LoadProjects();
        }

        private void LoadProjects()
        {
            var projects = _dbContext.Projects.ToList();

            if (!projects.Any())
            {
                var defaultProject = new Project { Name = "Default Project" };
                _dbContext.Projects.Add(defaultProject);
                _dbContext.SaveChanges();
                projects.Add(defaultProject);
            }

            Projects = new ObservableCollection<ProjectViewModel>(
                projects.Select(p => new ProjectViewModel(p)));
        }

        [RelayCommand]
        private void OpenProject(ProjectViewModel project)
        {
            _mainViewModel.NavigateToWorkspace(project);
        }

        [RelayCommand]
        private void CreateProject()
        {
            var newProject = new Project { Name = "New Project" };
            _dbContext.Projects.Add(newProject);
            _dbContext.SaveChanges();
            
            var vm = new ProjectViewModel(newProject);
            Projects.Add(vm);
            OpenProject(vm);
        }
        
        [RelayCommand]
        private void DeleteProject(ProjectViewModel project)
        {
            if (project == null) return;
            
            _dbContext.Projects.Remove(project.GetEntity());
            _dbContext.SaveChanges();
            Projects.Remove(project);
        }

        [RelayCommand]
        private void OpenSyncSettings()
        {
            _mainViewModel.NavigateToSyncSettings();
        }
    }
}
