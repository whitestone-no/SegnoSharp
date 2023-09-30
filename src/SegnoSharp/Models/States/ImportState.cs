using Microsoft.Extensions.Options;
using System.IO;
using Whitestone.SegnoSharp.Common.Models.Configuration;

namespace Whitestone.SegnoSharp.Models.States
{
    public class ImportState
    {
        public ImportState(IOptions<CommonConfig> commonConfig)
        {
            SelectedFolder = Directory.Exists(commonConfig.Value.MusicPath) ? new DirectoryInfo(commonConfig.Value.MusicPath) : null;
            MusicFolder = SelectedFolder;
        }

        public DirectoryInfo SelectedFolder { get; set; }
        public bool ImportSubfolders { get; set; }
        public SelectedFile[] SelectedFiles { get; set; }
        public DirectoryInfo MusicFolder { get; set; }
    }

    public class SelectedFile
    {
        private bool _import = true;
        private bool _importToPublicLibrary = true;
        private bool _importToPlaylist = true;

        public bool Import
        {
            get => _import;
            set
            {
                if (!value)
                {
                    _importToPlaylist = false;
                    _importToPublicLibrary = false;
                }

                _import = value;
            }
        }

        public bool ImportToPublicLibrary
        {
            get => _importToPublicLibrary;
            set
            {
                if (value && !_import)
                {
                    _import = true;
                }

                _importToPublicLibrary = value;
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