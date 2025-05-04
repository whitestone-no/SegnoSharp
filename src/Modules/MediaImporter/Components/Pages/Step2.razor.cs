using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Whitestone.SegnoSharp.Database.Extensions;
using Whitestone.SegnoSharp.Modules.MediaImporter.Models;
using Whitestone.SegnoSharp.Modules.MediaImporter.Models.Persistent;

namespace Whitestone.SegnoSharp.Modules.MediaImporter.Components.Pages
{
    public partial class Step2
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private ImportState ImporterState { get; set; }
        [Inject] private MediaImporterSettings Settings { get; set; }

        private string ErrorMessage { get; set; }

        private bool _loading;

        private bool AllImport
        {
            get => ImporterState.SelectedFiles.All(f => f.Import);
            set
            {
                foreach (SelectedFile file in ImporterState.SelectedFiles)
                {
                    file.Import = value;
                }
            }
        }

        private bool AllImportPlaylist
        {
            get => ImporterState.SelectedFiles.All(f => f.ImportToPlaylist);
            set
            {
                foreach (SelectedFile file in ImporterState.SelectedFiles)
                {
                    file.ImportToPlaylist = value;
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            _loading = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                if (ImporterState.SelectedFiles is { Length: > 0 })
                {
                    return;
                }

                if (ImporterState.SelectedFolder?.Parent == null)
                {
                    ErrorMessage = "No folder selected.";
                    return;
                }

                string[] extensions = [ ".mp3", ".flac" ];
                List<FileInfo> files = ImporterState.SelectedFolder.EnumerateFiles("*",
                        ImporterState.ImportSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(f => extensions.Contains(f.Extension))
                    .ToList();

                if (files.Count == 0)
                {
                    ErrorMessage = "No music files found in the selected path. Please redo the previous step.";
                    return;
                }

                if (files.Count > 1000)
                {
                    ErrorMessage = "More than 1000 files in selected folder. Please select a folder with fewer files.";
                    return;
                }

                ImporterState.SelectedFiles = files.Select(f => new SelectedFile
                {
                    File = f,
                    Filename = f.FullName.TrimStart(ImporterState.SelectedFolder.FullName).TrimStart('\\')
                }).ToArray();
            }
            finally
            {
                _loading = false;
            }
        }

        private void OnNextClick()
        {
            NavigationManager.NavigateTo("admin/mediaimporter/step3");
        }

        private void OnBackClick()
        {
            NavigationManager.NavigateTo("admin/mediaimporter/step1");
        }
    }
}
