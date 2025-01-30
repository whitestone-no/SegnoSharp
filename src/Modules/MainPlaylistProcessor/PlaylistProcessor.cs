using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.MainPlaylistProcessor
{
    internal class PlaylistProcessor : IPlaylistProcessor
    {
        public string Name => "Advanced Processor";
        public PlaylistProcessorSettings Settings { get; set; } = new MainPlaylistProcessorSettings();

        public Task<TrackStreamInfo> GetNextTrackAsync(CancellationToken cancellationToken)
        {
            /*
            var maxMinutesToLookBack = 0;

            if (_settings.MinutesBetweenAlbumRepeat > _settings.MinutesBetweenArtistRepeat &&
                _settings.MinutesBetweenAlbumRepeat > _settings.MinutesBetweenTrackRepeat)
            {
                maxMinutesToLookBack = _settings.MinutesBetweenAlbumRepeat;
            }
            else if (_settings.MinutesBetweenArtistRepeat > _settings.MinutesBetweenAlbumRepeat &&
                     _settings.MinutesBetweenArtistRepeat > _settings.MinutesBetweenTrackRepeat)
            {
                maxMinutesToLookBack = _settings.MinutesBetweenArtistRepeat;
            }

            else if (_settings.MinutesBetweenTrackRepeat > _settings.MinutesBetweenAlbumRepeat &&
                     _settings.MinutesBetweenTrackRepeat > _settings.MinutesBetweenArtistRepeat)
            {
                maxMinutesToLookBack = _settings.MinutesBetweenTrackRepeat;
            }

            double queueDuration = await dbContext.StreamQueue.SumAsync(q => q.TrackStreamInfo.Track.Duration.TotalMinutes, cancellationToken: cancellationToken);
            if (queueDuration < maxMinutesToLookBack)
            {

            }
            */

            return Task.FromResult<TrackStreamInfo>(null);
        }
    }
}
