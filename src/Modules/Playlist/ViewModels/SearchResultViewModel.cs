namespace Whitestone.SegnoSharp.Modules.Playlist.ViewModels
{
    public class SearchResultViewModel
    {
        public string AlbumTitle { get; set; }
        public string TrackTitle { get; set; }
        public string AlbumArtists { get; set; }
        public string TrackArtists { get; set; }
        public ushort Length { get; set; }

        public string Artists => string.IsNullOrEmpty(TrackArtists) ? AlbumArtists : TrackArtists;
    }
}
