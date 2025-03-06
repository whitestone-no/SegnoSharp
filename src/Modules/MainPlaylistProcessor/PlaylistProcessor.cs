using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.MainPlaylistProcessor
{
    internal class PlaylistProcessor(
        IRandomGenerator randomGenerator,
        IDbContextFactory<SegnoSharpDbContext> dbContextFactory,
        ILogger<PlaylistProcessor> log,
        ISystemClock systemClock) : IPlaylistProcessor
    {
        public string Name => "Advanced Processor";
        public PlaylistProcessorSettings Settings { get; set; } = new MainPlaylistProcessorSettings();

        public async Task<TrackStreamInfo> GetNextTrackAsync(CancellationToken cancellationToken)
        {
            await using SegnoSharpDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            DateTime now = systemClock.Now;

            // Calculate queue end time more efficiently
            var queueInfo = await dbContext.StreamQueue
                .AsNoTracking()
                .Select(q => new { q.TrackStreamInfo.Track.Length })
                .ToListAsync(cancellationToken);

            int queueSum = queueInfo.Sum(q => q.Length);

            // Get currently playing track info more efficiently
            var currentlyPlaying = await dbContext.StreamHistory
                .AsNoTracking()
                .OrderByDescending(h => h.Played)
                .Select(h => new { h.Played, h.TrackStreamInfo.Track.Length })
                .FirstOrDefaultAsync(cancellationToken);

            DateTime currentlyPlayingEnding = now;
            if (currentlyPlaying != null)
            {
                DateTime currentlyPlayingEndingTemp = currentlyPlaying.Played.AddSeconds(currentlyPlaying.Length);
                currentlyPlayingEnding = currentlyPlayingEndingTemp < now ? now : currentlyPlayingEndingTemp;
                TimeSpan currentlyPlayingDeltaNow = currentlyPlayingEnding - now;

                if (currentlyPlayingDeltaNow.TotalSeconds > 0)
                {
                    queueSum += (int)currentlyPlayingDeltaNow.TotalSeconds;
                }
            }

            DateTime endOfQueue = now.AddSeconds(queueSum);

            // Calculate cutoff times
            var settings = (MainPlaylistProcessorSettings)Settings;
            DateTime trackRepeatCutoff = endOfQueue.AddMinutes(settings.MinutesBetweenTrackRepeat * -1);
            DateTime albumRepeatCutoff = endOfQueue.AddMinutes(settings.MinutesBetweenAlbumRepeat * -1);
            DateTime artistRepeatCutoff = endOfQueue.AddMinutes(settings.MinutesBetweenArtistRepeat * -1);

            // Get excluded tracks, albums, and artists separately
            List<int> trackExclusions = await dbContext.TrackStreamInfos
                .AsNoTracking()
                .Where(tsi =>
                    // Check queue
                    tsi.StreamQueue.Any(sq =>
                        currentlyPlayingEnding.AddSeconds(
                            tsi.StreamQueue
                                .Where(q => q.SortOrder <= sq.SortOrder)
                                .Sum(q => q.TrackStreamInfo.Track.Length)
                        ) > trackRepeatCutoff)
                    // Check history if needed
                    || (settings.MinutesBetweenTrackRepeat * 60 >= queueSum
                        && tsi.StreamHistory.Any(h => h.Played > trackRepeatCutoff))
                )
                .Select(tsi => tsi.TrackId)
                .Distinct()
                .ToListAsync(cancellationToken);

            List<int> albumExclusions = await dbContext.TrackStreamInfos
                .AsNoTracking()
                .Where(tsi =>
                    tsi.StreamQueue.Any(sq =>
                        currentlyPlayingEnding.AddSeconds(
                            tsi.StreamQueue
                                .Where(q => q.SortOrder <= sq.SortOrder)
                                .Sum(q => q.TrackStreamInfo.Track.Length)
                        ) > albumRepeatCutoff)
                    || (settings.MinutesBetweenAlbumRepeat * 60 >= queueSum
                        && tsi.StreamHistory.Any(h => h.Played > albumRepeatCutoff))
                )
                .Select(tsi => tsi.Track.Disc.AlbumId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var artistExclusions = await dbContext.TrackStreamInfos
                .AsNoTracking()
                .Where(tsi =>
                    tsi.StreamQueue.Any(sq =>
                        currentlyPlayingEnding.AddSeconds(
                            tsi.StreamQueue
                                .Where(q => q.SortOrder <= sq.SortOrder)
                                .Sum(q => q.TrackStreamInfo.Track.Length)
                        ) > artistRepeatCutoff)
                    || (settings.MinutesBetweenArtistRepeat * 60 >= queueSum
                        && tsi.StreamHistory.Any(h => h.Played > artistRepeatCutoff))
                )
                .Select(tsi => new
                {
                    HasTrackArtists = tsi.Track.TrackPersonGroupPersonRelations.Any(r => 
                        r.PersonGroup.PersonGroupStreamInfo != null &&
                        r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist),
                    TrackArtistIds = tsi.Track.TrackPersonGroupPersonRelations
                        .Where(r => r.PersonGroup.PersonGroupStreamInfo != null &&
                                   r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                        .SelectMany(r => r.Persons.Select(p => p.Id)),
                    AlbumArtistIds = tsi.Track.Disc.Album.AlbumPersonGroupPersonRelations
                        .Where(r => r.PersonGroup.PersonGroupStreamInfo != null &&
                                   r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                        .SelectMany(r => r.Persons.Select(p => p.Id))
                })
                .ToListAsync(cancellationToken);

            List<int> excludedArtistIds = artistExclusions
                .SelectMany(e => e.HasTrackArtists ? e.TrackArtistIds : e.AlbumArtistIds)
                .Distinct()
                .ToList();

            // Get eligible tracks with a single query
            var eligibleTracks = await dbContext.TrackStreamInfos
                .AsNoTracking()
                .Where(tsi =>
                    !trackExclusions.Contains(tsi.TrackId) &&
                    !albumExclusions.Contains(tsi.Track.Disc.AlbumId) &&
                    !tsi.Track.TrackPersonGroupPersonRelations.Any(r =>
                        r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist &&
                        r.Persons.Any(p => excludedArtistIds.Contains(p.Id))) &&
                    (tsi.Track.TrackPersonGroupPersonRelations.Any() || !tsi.Track.Disc.Album.AlbumPersonGroupPersonRelations.Any(r =>
                            r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist &&
                            r.Persons.Any(p => excludedArtistIds.Contains(p.Id))))
                )
                .Select(tsi => new { tsi.TrackId, tsi.Weight })
                .ToListAsync(cancellationToken);

            if (eligibleTracks.Count == 0)
            {
                log.LogInformation("No tracks found that meet all the rules");
                return null;
            }

            // Select track based on weight or random
            int selectedTrackId;
            if (settings.UseWeights)
            {
                int weightSum = eligibleTracks.Sum(t => t.Weight);
                int rnd = randomGenerator.GetInt(weightSum);

                var runningTotal = 0;
                selectedTrackId = eligibleTracks
                    .Select(t => new { t.TrackId, RunningTotal = runningTotal += t.Weight })
                    .First(t => t.RunningTotal >= rnd)
                    .TrackId;
            }
            else
            {
                int randomNumber = randomGenerator.GetInt(eligibleTracks.Count);
                selectedTrackId = eligibleTracks
                    .OrderBy(t => t.TrackId)
                    .Skip(randomNumber)
                    .First()
                    .TrackId;
            }

            // Get final track
            return await dbContext.TrackStreamInfos
                .AsNoTracking()
                .FirstOrDefaultAsync(tsi => tsi.TrackId == selectedTrackId, cancellationToken);
        }
    }
}
