using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;
using Whitestone.SegnoSharp.Common.Models;

namespace Whitestone.SegnoSharp.Modules.MainPlaylistProcessor
{
    public class MainPlaylistProcessorSettings : PlaylistProcessorSettings
    {
        [Persist]
        [DefaultValue(5)]
        [Description("Minimum number of minutes to wait before repeating an album.")]
        public int MinutesBetweenAlbumRepeat { get; set; }
        
        [Persist]
        [DefaultValue(105)]
        [Description("Minimum number of minutes to wait before repeating a track.")]
        public int MinutesBetweenTrackRepeat { get; set; }

        [Persist]
        [DefaultValue(10)]
        [Description("Minimum number of minutes to wait before repeating an artist.")]
        public int MinutesBetweenArtistRepeat { get; set; }

        [Persist]
        [DefaultValue(true)]
        [Description("Use a weighted selection algorithm to select the next track.")]
        public bool UseWeights { get; set; }
    }
}
