using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Modules.Playlist.Models
{
    public class PlaylistSettings
    {
        [Persist]
        [DefaultValue(3)]
        public int MinimumNumberOfSongs { get; set; }

        [Persist]
        [DefaultValue(15)]
        public int MinimumTotalDuration { get; set; }

        [Persist]
        [DefaultValue(5)]
        public int MinutesBetweenAlbumRepeat { get; set; }
        [Persist]
        [DefaultValue(105)]
        public int MinutesBetweenTrackRepeat { get; set; }
        [Persist]
        [DefaultValue(10)]
        public int MinutesBetweenArtistRepeat { get; set; }
    }
}
