using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using Avalonia.Threading;
using HarManager.ViewModels;
using HarManager.Helpers;
using System;
using System.Linq;

namespace HarManager.Views
{
    public partial class WorkspaceView : UserControl
    {
        private FoldingManager? _requestFoldingManager;
        private FoldingManager? _responseFoldingManager;
        private readonly JsonFoldingStrategy _foldingStrategy = new();

        public WorkspaceView()
        {
            InitializeComponent();

            AddHandler(DragDrop.DropEvent, OnDrop);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            
            // Wait for template application
            Dispatcher.UIThread.Post(() =>
            {
                if (RequestJsonEditor != null)
                {
                    _requestFoldingManager = FoldingManager.Install(RequestJsonEditor.TextArea);
                    RequestJsonEditor.TextChanged += (s, e) => UpdateRequestFoldings();
                    // Initial update
                    UpdateRequestFoldings();
                }

                if (ResponseJsonEditor != null)
                {
                    _responseFoldingManager = FoldingManager.Install(ResponseJsonEditor.TextArea);
                    ResponseJsonEditor.TextChanged += (s, e) => UpdateResponseFoldings();
                    // Initial update
                    UpdateResponseFoldings();
                }
            });
        }

        private void UpdateRequestFoldings()
        {
            if (_requestFoldingManager != null && RequestJsonEditor?.Document != null)
            {
                _foldingStrategy.UpdateFoldings(_requestFoldingManager, RequestJsonEditor.Document);
            }
        }

        private void UpdateResponseFoldings()
        {
            if (_responseFoldingManager != null && ResponseJsonEditor?.Document != null)
            {
                _foldingStrategy.UpdateFoldings(_responseFoldingManager, ResponseJsonEditor.Document);
            }
        }

        private void OnDataGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                // Prevent deletion if the user is editing a cell (Source would be the TextBox)
                if (e.Source is TextBox) return;

                if (DataContext is WorkspaceViewModel vm && sender is DataGrid grid)
                {
                    vm.DeleteEntriesCommand.Execute(grid.SelectedItems);
                }
            }
        }

        private void OnDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is WorkspaceViewModel vm && sender is DataGrid grid)
            {
                vm.SelectedEntries = grid.SelectedItems;
            }
        }
        private void OnDragOver(object? sender, DragEventArgs e)
        {
#pragma warning disable CS0618 // DragEventArgs.Data is obsolete
            if (e.Data.Contains(DataFormats.FileNames))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
#pragma warning restore CS0618
        }

        private async void OnDrop(object? sender, DragEventArgs e)
        {
#pragma warning disable CS0618 // DragEventArgs.Data is obsolete
            if (DataContext is WorkspaceViewModel vm && e.Data.Contains(DataFormats.FileNames))
            {
                var files = e.Data.GetFileNames();
                if (files != null)
                {
                    await vm.LoadHarFilesAsync(files);
                }
            }
#pragma warning restore CS0618
        }

        private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && DataContext is WorkspaceViewModel vm)
            {
                vm.SaveChangesCommand.Execute(null);
            }
        }
    }
}

