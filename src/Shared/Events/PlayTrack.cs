using Whitestone.SegnoSharp.Common.Models;

namespace Whitestone.SegnoSharp.Common.Events
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
