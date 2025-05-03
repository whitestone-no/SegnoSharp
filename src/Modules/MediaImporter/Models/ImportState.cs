using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Shared.Models.Configuration;
using Whitestone.SegnoSharp.Modules.MediaImporter.ViewModels;

namespace Whitestone.SegnoSharp.Modules.MediaImporter.Models
{
    public class ImportState
    {
        public ImportState(IOptions<SiteConfig> siteConfig)
        {
            SelectedFolder = Directory.Exists(siteConfig.Value.MusicPath) ? new DirectoryInfo(siteConfig.Value.MusicPath) : null;
            MusicFolder = SelectedFolder;
        }

        public DirectoryInfo SelectedFolder { get; set; }
        public bool ImportSubfolders { get; set; }
        public SelectedFile[] SelectedFiles { get; set; }
        public DirectoryInfo MusicFolder { get; set; }
        public List<AlbumViewModel> AlbumsToImport { get; set; }
    }

    public class SelectedFile
    {
        private bool _import = true;
        private bool _importToPlaylist = true;

        public bool Import
        {
            get => _import;
            set
            {
                if (!value)
                {
                    _importToPlaylist = false;
                }

                _import = value;
            }
        }

        public bool ImportToPlaylist
        {
            get => _importToPlaylist;
            set
            {
                if (value && !_import)
                {
                    _import = true;
                }

                _importToPlaylist = value;

            }
        }

        public string Filename { get; set; }
        public FileInfo File { get; set; }
    }
}