using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.Playlist.Processors
{
    public class AdvancedProcessor : IPlaylistProcessor
    {
        public PlaylistProcessorSettings Settings { get; set; } = new AdvancedProcessorSettings();

        public Task<TrackStreamInfo> GetNextTrackAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<TrackStreamInfo>(null);
        }
    }

    public class AdvancedProcessorSettings : PlaylistProcessorSettings
    {
    }
}
