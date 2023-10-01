using System.IO;

namespace Whitestone.SegnoSharp.Common.Models
{
    public class Tags
    {
        public string Album { get; set; }
        public string AlbumArtist { get; set; }
        public string Artist { get; set; }
        public string Composer { get; set; }
        public byte Disc { get; set; }
        public string Genre { get; set; }
        public string Title { get; set; }
        public ushort TrackNumber { get; set; }
        public ushort Year { get; set; }
        public double Duration { get; set; }
        public string File { get; set; }
    }
}
