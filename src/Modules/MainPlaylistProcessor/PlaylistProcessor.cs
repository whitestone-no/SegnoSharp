using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models;
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


            // Third attempt:

            // Get the DateTime for when the current queue (plus currently playing track) will be finished:
            // Sum all track lengths in queue and add the length of the currently playing track, then add that to DateTime.Now

            int queueSum = await dbContext.StreamQueue
                .AsNoTracking()
                .Include(q => q.TrackStreamInfo)
                .ThenInclude(tsi => tsi.Track)
                .SumAsync(q => q.TrackStreamInfo.Track.Length, cancellationToken);

            StreamHistory currentlyPlaying = await dbContext.StreamHistory
                .AsNoTracking()
                .Include(h => h.TrackStreamInfo)
                .ThenInclude(tsi => tsi.Track)
                .OrderByDescending(h => h.Played)
                .FirstOrDefaultAsync(cancellationToken);

            DateTime currentlyPlayingEnding = now;

            if (currentlyPlaying != null)
            {
                DateTime currentlyPlayingEndingTemp = currentlyPlaying.Played.AddSeconds(currentlyPlaying.TrackStreamInfo.Track.Length);
                currentlyPlayingEnding = currentlyPlayingEndingTemp < now ? now : currentlyPlayingEndingTemp;
                TimeSpan currentlyPlayingDeltaNow = currentlyPlayingEnding - now;

                if (currentlyPlayingDeltaNow.TotalSeconds > 0)
                {
                    queueSum += (int)currentlyPlayingDeltaNow.TotalSeconds;
                }
            }

            DateTime endOfQueue = now.AddSeconds(queueSum);

            // From this new base DateTime, get the DateTimes for each track/album/artist rule
            // This is how far back we have to search in the queue and history

            DateTime trackRepeatCutoff = endOfQueue.AddMinutes(((MainPlaylistProcessorSettings)Settings).MinutesBetweenTrackRepeat * -1);
            DateTime albumRepeatCutoff = endOfQueue.AddMinutes(((MainPlaylistProcessorSettings)Settings).MinutesBetweenAlbumRepeat * -1);
            DateTime artistRepeatCutoff = endOfQueue.AddMinutes(((MainPlaylistProcessorSettings)Settings).MinutesBetweenArtistRepeat * -1);

            // Everything in queue
            List<StreamQueue> queue = await dbContext.StreamQueue
                .AsNoTracking()
                .Include(t => t.TrackStreamInfo)
                .ThenInclude(t => t.Track)
                .ThenInclude(t => t.TrackPersonGroupPersonRelations)
                .ThenInclude(r => r.Persons)
                .Include(t => t.TrackStreamInfo)
                .ThenInclude(t => t.Track)
                .ThenInclude(t => t.TrackPersonGroupPersonRelations)
                .ThenInclude(r => r.PersonGroup)
                .ThenInclude(g => g.PersonGroupStreamInfo)
                .Include(t => t.TrackStreamInfo)
                .ThenInclude(t => t.Track)
                .ThenInclude(t => t.Disc)
                .ThenInclude(d => d.Album)
                .ThenInclude(a => a.AlbumPersonGroupPersonRelations)
                .ThenInclude(r => r.Persons)
                .Include(t => t.TrackStreamInfo)
                .ThenInclude(t => t.Track)
                .ThenInclude(t => t.Disc)
                .ThenInclude(d => d.Album)
                .ThenInclude(a => a.AlbumPersonGroupPersonRelations)
                .ThenInclude(r => r.PersonGroup)
                .ThenInclude(g => g.PersonGroupStreamInfo)
                .OrderBy(t => t.SortOrder)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);


            var queueRunningSum = 0;

            var queueWithCalculatedPlayed = queue
                .Select(q => new
                {
                    QueueItem = q,
                    RunningTotal = queueRunningSum += q.TrackStreamInfo.Track.Length,
                    Played = currentlyPlayingEnding.AddSeconds(queueRunningSum)
                })
                .ToList();

            // Get all tracks from queue and history that are newer than the DateTime for each rule
            // For track rule: Just get the track IDs

            List<int> trackExclusionList = queueWithCalculatedPlayed
                .Where(q => q.Played > trackRepeatCutoff)
                .Select(q => q.QueueItem.TrackStreamInfo.TrackId)
                .ToList();

            // Check if queue length is less than track rule. Also use history if it is.
            if (((MainPlaylistProcessorSettings)Settings).MinutesBetweenTrackRepeat * 60 >= queueSum)
            {
                trackExclusionList.AddRange(await dbContext.StreamHistory
                    .AsNoTracking()
                    .Where(h => h.Played > trackRepeatCutoff)
                    .Include(t => t.TrackStreamInfo)
                    .Select(h => h.TrackStreamInfo.TrackId)
                    .ToListAsync(cancellationToken));
            }

            trackExclusionList = trackExclusionList.Distinct().ToList(); // Remove duplicates

            // Get all albums from queue and history that are newer than the DateTime for each rule
            // For album rule: Get the album IDs

            List<int> albumExclusionList = queueWithCalculatedPlayed
                .Where(q => q.Played > albumRepeatCutoff)
                .Select(q => q.QueueItem.TrackStreamInfo.Track.Disc.AlbumId)
                .ToList();

            if (((MainPlaylistProcessorSettings)Settings).MinutesBetweenAlbumRepeat * 60 >= queueSum)
            {
                albumExclusionList.AddRange(await dbContext.StreamHistory
                    .AsNoTracking()
                    .Where(h => h.Played > albumRepeatCutoff)
                    .Include(h => h.TrackStreamInfo)
                    .ThenInclude(tsi => tsi.Track)
                    .ThenInclude(t => t.Disc)
                    .Select(h => h.TrackStreamInfo.Track.Disc.AlbumId)
                    .ToListAsync(cancellationToken));

                albumExclusionList = albumExclusionList.Distinct().ToList(); // Remove duplicates
            }

            // Get all artists from queue and history that are newer than the DateTime for each rule
            // For artist rule: Get the artist IDs, but if a track does not have an artist use the album artist

            List<int> artistExclusionList = queueWithCalculatedPlayed
                .Where(q => q.Played > artistRepeatCutoff)
                .Select(q => q.QueueItem)
                .SelectMany(sq =>
                {
                    List<int> trackPersonIds = sq.TrackStreamInfo.Track.TrackPersonGroupPersonRelations
                        .Where(rel => rel.PersonGroup.PersonGroupStreamInfo is { IncludeInAutoPlaylist: true })
                        .SelectMany(rel => rel.Persons ?? Enumerable.Empty<Person>())
                        .Select(p => p.Id)
                        .ToList();

                    if (trackPersonIds.Count > 0)
                    {
                        return trackPersonIds;
                    }

                    List<int> albumPersonIds = sq.TrackStreamInfo.Track.Disc.Album.AlbumPersonGroupPersonRelations
                        .Where(rel => rel.PersonGroup.PersonGroupStreamInfo is { IncludeInAutoPlaylist: true })
                        .SelectMany(rel => rel.Persons ?? new List<Person>())
                        .Select(p => p.Id)
                        .ToList();

                    return albumPersonIds;
                })
                .Distinct()
                .ToList();

            if (((MainPlaylistProcessorSettings)Settings).MinutesBetweenArtistRepeat * 60 >= queueSum)
            {
                List<StreamHistory> historyItems = await dbContext.StreamHistory
                    .AsNoTracking()
                    .Include(t => t.TrackStreamInfo)
                    .ThenInclude(t => t.Track)
                    .ThenInclude(t => t.TrackPersonGroupPersonRelations)
                    .ThenInclude(r => r.Persons)
                    .Include(t => t.TrackStreamInfo)
                    .ThenInclude(t => t.Track)
                    .ThenInclude(t => t.TrackPersonGroupPersonRelations)
                    .ThenInclude(r => r.PersonGroup)
                    .ThenInclude(g => g.PersonGroupStreamInfo)
                    .Include(t => t.TrackStreamInfo)
                    .ThenInclude(t => t.Track)
                    .ThenInclude(t => t.Disc)
                    .ThenInclude(d => d.Album)
                    .ThenInclude(a => a.AlbumPersonGroupPersonRelations)
                    .ThenInclude(r => r.Persons)
                    .Include(t => t.TrackStreamInfo)
                    .ThenInclude(t => t.Track)
                    .ThenInclude(t => t.Disc)
                    .ThenInclude(d => d.Album)
                    .ThenInclude(a => a.AlbumPersonGroupPersonRelations)
                    .ThenInclude(r => r.PersonGroup)
                    .ThenInclude(g => g.PersonGroupStreamInfo)
                    .AsSplitQuery()
                    .Where(h => h.Played > artistRepeatCutoff)
                    .ToListAsync(cancellationToken);

                artistExclusionList.AddRange(historyItems.SelectMany(sq =>
                    {
                        List<int> trackPersonIds = sq.TrackStreamInfo.Track.TrackPersonGroupPersonRelations
                            .Where(rel => rel.PersonGroup.PersonGroupStreamInfo is { IncludeInAutoPlaylist: true })
                            .SelectMany(rel => rel.Persons ?? Enumerable.Empty<Person>())
                            .Select(p => p.Id)
                            .ToList();

                        if (trackPersonIds.Count > 0)
                        {
                            return trackPersonIds;
                        }

                        List<int> albumPersonIds = sq.TrackStreamInfo.Track.Disc.Album.AlbumPersonGroupPersonRelations
                            .Where(rel => rel.PersonGroup.PersonGroupStreamInfo is { IncludeInAutoPlaylist: true })
                            .SelectMany(rel => rel.Persons ?? new List<Person>())
                            .Select(p => p.Id)
                            .ToList();

                        return albumPersonIds;
                    })
                    .Distinct()
                    .ToList());

                artistExclusionList = artistExclusionList.Distinct().ToList();
            }

            // Finally get all tracks where track/album/artist is not in the list above
            var totalList = await dbContext.TrackStreamInfos
                .AsNoTracking()
                .Include(tsi => tsi.Track)
                .ThenInclude(t => t.TrackPersonGroupPersonRelations)
                .ThenInclude(r => r.Persons)
                .Include(tsi => tsi.Track)
                .ThenInclude(t => t.TrackPersonGroupPersonRelations)
                .ThenInclude(r => r.PersonGroup)
                .ThenInclude(g => g.PersonGroupStreamInfo)
                .Include(tsi => tsi.Track)
                .ThenInclude(t => t.Disc)
                .ThenInclude(d => d.Album)
                .ThenInclude(a => a.AlbumPersonGroupPersonRelations)
                .ThenInclude(r => r.Persons)
                .Include(tsi => tsi.Track)
                .ThenInclude(t => t.Disc)
                .ThenInclude(d => d.Album)
                .ThenInclude(a => a.AlbumPersonGroupPersonRelations)
                .ThenInclude(r => r.PersonGroup)
                .ThenInclude(g => g.PersonGroupStreamInfo)
                .AsSplitQuery()
                .Where(tsi => !trackExclusionList.Contains(tsi.TrackId) &&
                              !albumExclusionList.Contains(tsi.Track.Disc.AlbumId) &&
                              
                              // Case 1: The track has person relations and none of them (where Included is true) contain an excluded person.
                              ((tsi.Track.TrackPersonGroupPersonRelations.Any() &&
                               !tsi.Track.TrackPersonGroupPersonRelations.Any(r =>
                                   r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist &&
                                   r.Persons.Any(p => artistExclusionList.Contains(p.Id))
                               ))
                              ||
                              // Case 2: The track has no person relations; so check the album's relations instead.
                              (!tsi.Track.TrackPersonGroupPersonRelations.Any() &&
                               !tsi.Track.Disc.Album.AlbumPersonGroupPersonRelations.Any(r =>
                                   r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist &&
                                   r.Persons.Any(p => artistExclusionList.Contains(p.Id))
                               )))

                )    
                .Select(tsi => new
                {
                    tsi.TrackId,
                    tsi.Weight
                })
                .ToListAsync(cancellationToken);

            if (totalList.Count == 0)
            {
                log.LogInformation("No tracks found that meet all the rules");
                return null;
            }

            // Then calculate the weights and select accordingly

            if (Settings is MainPlaylistProcessorSettings { UseWeights: false })
            {
                int totalTracks = totalList.Count;
                int randomNumber = randomGenerator.GetInt(totalTracks);

                var track = totalList
                    .OrderBy(tsi => tsi.TrackId)
                    .Skip(randomNumber)
                    .FirstOrDefault();

                return await dbContext.TrackStreamInfos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(tsi => tsi.TrackId == track.TrackId, cancellationToken);
            }

            var runningTotal = 0;
            int weightSum = totalList.Sum(tsi => tsi.Weight);
            int rnd = randomGenerator.GetInt(weightSum);

            var t = totalList
                .Select(tsi => new
                {
                    tsi.TrackId,
                    RunningTotal = runningTotal += tsi.Weight
                })
                .FirstOrDefault(ts => ts.RunningTotal >= rnd);

            return await dbContext.TrackStreamInfos
                .AsNoTracking()
                .FirstOrDefaultAsync(tsi => tsi.TrackId == t.TrackId, cancellationToken);

        }
    }
}
