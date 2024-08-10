using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Whitestone.SegnoSharp.Common.Extensions;
using Whitestone.SegnoSharp.Database.Extensions;
using Whitestone.SegnoSharp.Models.States;

namespace Whitestone.SegnoSharp.Components.Pages.Admin.Importer
{
    public partial class Step2
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private ImportState ImporterState { get; set; }

        private string ErrorMessage { get; set; }

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

        protected override void OnInitialized()
        {
            if (ImporterState.SelectedFiles != null &&
                ImporterState.SelectedFiles.Any())
            {
                base.OnInitialized();
                return;
            }

            if (ImporterState.SelectedFolder?.Parent == null)
            {
                ErrorMessage = "No folder selected.";
                base.OnInitialized();
                return;
            }

            string[] extensions = { ".wma", ".mp3", ".flac" };
            List<FileInfo> files = ImporterState.SelectedFolder.EnumerateFiles("*",
                    ImporterState.ImportSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(f => extensions.Contains(f.Extension))
                .ToList();

            if (files.Count > 1000)
            {
                ErrorMessage = "More than 1000 files in selected folder. Please select a folder with fewer files.";
                base.OnInitialized();
                return;
            }

            ImporterState.SelectedFiles = files.Select(f => new SelectedFile
            {
                File = f,
                Filename = f.FullName.TrimStart(ImporterState.SelectedFolder.FullName).TrimStart('\\')
            }).ToArray();

            base.OnInitialized();
        }

        private void OnNextClick()
        {
            NavigationManager.NavigateTo("/admin/import/step-3");
        }

        private void OnBackClick()
        {
            NavigationManager.NavigateTo("/admin/import");
        }
    }
}
