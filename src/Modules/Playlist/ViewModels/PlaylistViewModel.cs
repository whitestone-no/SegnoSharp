using System;

namespace Whitestone.SegnoSharp.Modules.Playlist.ViewModels
{
    public class PlaylistViewModel
    {
        public string AlbumTitle { get; set; }
        public string TrackTitle { get; set; }
        public string AlbumArtists { get; set; }
        public string TrackArtists { get; set; }
        public TimeSpan Length { get; set; }
        public uint QueueId { get; set; }
        public ushort SortOrder { get; set; }
        public string Artists => string.IsNullOrEmpty(TrackArtists) ? AlbumArtists : TrackArtists;
    }
}
