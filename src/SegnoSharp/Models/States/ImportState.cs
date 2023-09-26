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
        public bool Import { get; set; } = true;
        public bool ImportToPublicLibrary { get; set; } = true;
        public bool ImportToPlaylist { get; set; } = true;
        public string Filename { get; set; }
        public FileInfo File { get; set; }
    }
}