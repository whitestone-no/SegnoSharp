using System;

namespace Whitestone.SegnoSharp.Modules.Playlist.ViewModels
{
    internal class HistoryViewModel
    {
        public string AlbumTitle { get; set; }
        public string TrackTitle { get; set; }
        public string AlbumArtists { get; set; }
        public string TrackArtists { get; set; }
        public TimeSpan Length { get; set; }
        public DateTime Played { get; set; }
        public int AlbumId { get; set; }
        public bool HasAlbumCover { get; set; }
        public bool CurrentlyPlaying { get; set; }

        public string Artists => string.IsNullOrEmpty(TrackArtists) ? AlbumArtists : TrackArtists;
    }
}
