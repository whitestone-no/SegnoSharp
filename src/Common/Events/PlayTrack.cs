using Whitestone.WASP.Common.Models;

namespace Whitestone.WASP.Common.Events
{
    public class PlayTrack
    {
        public Track Track { get; }

        public PlayTrack(Track track)
        {
            Track = track;
        }
    }
}
