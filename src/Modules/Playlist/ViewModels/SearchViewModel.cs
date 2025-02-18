namespace Whitestone.SegnoSharp.Modules.Playlist.ViewModels
{
    public class SearchViewModel
    {
        public string SearchTerm { get; set; }
        public bool SearchForAlbum { get; set; } = true;
        public bool SearchForTrack { get; set; } = true;
        public bool SearchForArtist { get; set; }
        public bool SearchForFilename { get; set; }
        public bool OnlyPublic { get; set; }
        public bool OnlyAutoPlaylist { get; set; }
    }
}
