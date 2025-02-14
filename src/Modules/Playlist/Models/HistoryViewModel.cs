using System;

namespace Whitestone.SegnoSharp.Modules.Playlist.Models
{
    internal class HistoryViewModel
    {
        public string Album { get; set; }
        public string Track { get; set; }
        public string Artist { get; set; }
        public DateTime Played { get; set; }
    }
}
