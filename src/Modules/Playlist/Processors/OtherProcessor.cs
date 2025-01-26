using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.Playlist.Processors
{
    public class OtherProcessor : IPlaylistProcessor
    {
        public PlaylistProcessorSettings Settings { get; set; } = new OtherProcessorSettings();

        public Task<TrackStreamInfo> GetNextTrackAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<TrackStreamInfo>(null);
        }
    }

    public class OtherProcessorSettings : PlaylistProcessorSettings
    {
    }
}
