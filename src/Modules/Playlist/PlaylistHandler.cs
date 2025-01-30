using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Common.Events;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Modules.Playlist.Models;
using Whitestone.SegnoSharp.Modules.Playlist.Processors;
using Track = Whitestone.SegnoSharp.Common.Models.Track;

namespace Whitestone.SegnoSharp.Modules.Playlist
{
    public class PlaylistHandler : IHostedService, IEventHandler<PlayerReady>, IEventHandler<PlayNextTrack>
    {
        private readonly IDbContextFactory<SegnoSharpDbContext> _dbContextFactory;
        private readonly IPersistenceManager _persistenceManager;
        private readonly ISystemClock _systemClock;
        private readonly ICambion _cambion;
        private readonly ILogger<PlaylistHandler> _log;
        private readonly List<IPlaylistProcessor> _playlistProcessors;
        private readonly PlaylistSettings _settings;
        // Ensure that the tasks for reading the playlist and updating the playlist don't step on each other's toes
        private readonly SemaphoreSlim _queueMutex = new(1);
        private readonly CancellationTokenSource _autoplaylistTaskCancellationTokenSource = new();
        private CancellationTokenSource _currentlyPlayingTaskCancellationTokenSource;

        public PlaylistHandler(
            ITagReader tagReader,
            IDbContextFactory<SegnoSharpDbContext> dbContextFactory,
            IPersistenceManager persistenceManager,
            ISystemClock systemClock,
            ICambion cambion,
            ILogger<PlaylistHandler> log,
            IEnumerable<IPlaylistProcessor> playlistProcessors,
            PlaylistSettings playlistSettings)
        {
            _dbContextFactory = dbContextFactory;
            _persistenceManager = persistenceManager;
            _systemClock = systemClock;
            _cambion = cambion;
            _log = log;
            _playlistProcessors = playlistProcessors.ToList();
            _settings = playlistSettings;

            cambion.Register(this);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _persistenceManager.RegisterAsync(_settings);

            foreach (IPlaylistProcessor playlistProcessor in _playlistProcessors)
            {
                if (playlistProcessor.Settings == null)
                {
                    throw new Exception($"Invalid playlist processor. Processor {playlistProcessor.GetType().FullName} is missing a Settings object");
                }

                await _persistenceManager.RegisterAsync(playlistProcessor.Settings);
            }

            // All playlist processors should now have values from database injected into the settings objects
            // Normalize the sort orders, and make sure that the `DefaultProcessor` always has the lowest sort order
            ushort counter = 1;

            foreach (IPlaylistProcessor playlistProcessor in _playlistProcessors.OrderBy(p => p.Settings.SortOrder))
            {
                if (playlistProcessor is DefaultProcessor)
                {
                    playlistProcessor.Settings.SortOrder = 0;
                }
                else
                {
                    playlistProcessor.Settings.SortOrder = counter++;
                }
            }

            // Start the task to automatically fill the playlist
            // Don't await it as it should be long-running, so just discard the Task object
            _ = AutoplaylistTask(_autoplaylistTaskCancellationTokenSource.Token);

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop any running tasks
            _autoplaylistTaskCancellationTokenSource?.Cancel();
            _currentlyPlayingTaskCancellationTokenSource?.Cancel();

            return Task.CompletedTask;
        }

        public void HandleEvent(PlayerReady input)
        {
            // Start timer that counts down from track.Duration and sends a new track to the player when current track reaches zero.
            // Do not await it as this should run in the background.

            if (_currentlyPlayingTaskCancellationTokenSource != null)
            {
                _log.LogWarning("{event} was fired while CurrentlyPlayingTask was already running. Doing nothing.", nameof(PlayerReady));
                return;
            }

            _currentlyPlayingTaskCancellationTokenSource = new CancellationTokenSource();

            _ = CurrentlyPlayingTask(_currentlyPlayingTaskCancellationTokenSource.Token);
        }

        private async Task AutoplaylistTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _queueMutex.WaitAsync(cancellationToken);

                    await using SegnoSharpDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                    if (dbContext.TrackStreamInfos.Any(t => t.IncludeInAutoPlaylist))
                    {
                        int tracksInQueue = await dbContext.StreamQueue.CountAsync(cancellationToken);
                        int totalQueueDuration = await dbContext.StreamQueue.SumAsync(q => q.TrackStreamInfo.Track.Length, cancellationToken);

                        while (tracksInQueue < _settings.MinimumNumberOfSongs && totalQueueDuration < _settings.MinimumTotalDuration * 60)
                        {
                            _log.LogDebug("Currently {tracksInQueue} tracks in queue (less than {minimumNumberOfSongs}) with a total duration of {totalQueueDuration} seconds (less than {minimumTotalDuration})", tracksInQueue, _settings.MinimumNumberOfSongs, totalQueueDuration, _settings.MinimumTotalDuration * 60);

                            TrackStreamInfo track = await GetNextTrackFromProcessors(cancellationToken);

                            if (track == null)
                            {
                                _log.LogWarning("Could not automatically add track to queue as no playlist processors returned a track");
                                break;
                            }

                            dbContext.TrackStreamInfos.Attach(track);

                            ushort maxSortOrder = await dbContext.StreamQueue.AnyAsync(cancellationToken)
                                ? await dbContext.StreamQueue
                                    .MaxAsync(q => q.SortOrder, cancellationToken)
                                : (ushort)0;

                            await dbContext.StreamQueue.AddAsync(new StreamQueue
                            {
                                SortOrder = (ushort)(maxSortOrder + 1),
                                TrackStreamInfo = track
                            }, cancellationToken);

                            await dbContext.SaveChangesAsync(cancellationToken);

                            tracksInQueue = await dbContext.StreamQueue.CountAsync(cancellationToken);
                            totalQueueDuration = await dbContext.StreamQueue.SumAsync(q => q.TrackStreamInfo.Track.Length, cancellationToken);
                        }
                    }
                    else
                    {
                        _log.LogWarning("Could not add tracks to playlist as no tracks are found");
                    }
                }
                catch (Exception e)
                {
                    _log.LogError(e, "Method {method} in {class} failed", nameof(AutoplaylistTask), nameof(PlaylistHandler));
                }
                finally
                {
                    _queueMutex.Release();
                }

                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
        }

        private async Task<TrackStreamInfo> GetNextTrackFromProcessors(CancellationToken cancellationToken)
        {
            TrackStreamInfo nextTrack = null;

            IEnumerable<IPlaylistProcessor> playlistProcessors = _playlistProcessors
                .Where(p => p.Settings is
                {
                    Enabled: true
                })
                .OrderByDescending(p => p.Settings.SortOrder);

            foreach (IPlaylistProcessor playlistProcessor in playlistProcessors)
            {
                nextTrack = await playlistProcessor.GetNextTrackAsync(cancellationToken);

                if (nextTrack != null)
                {
                    break;
                }
            }

            return nextTrack;
        }

        public async Task CurrentlyPlayingTask(CancellationToken cancellationToken)
        {
            try
            {
                _log.LogDebug("{method} is started", nameof(CurrentlyPlayingTask));

                while (!cancellationToken.IsCancellationRequested)
                {
                    StreamQueue queuetrack;
                    try
                    {
                        await using SegnoSharpDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                        await _queueMutex.WaitAsync(cancellationToken);

                        queuetrack = await dbContext.StreamQueue
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
                            .FirstOrDefaultAsync(cancellationToken);

                        if (queuetrack != null)
                        {
                            DateTime now = _systemClock.Now;

                            queuetrack.TrackStreamInfo.LastPlayed = now;
                            queuetrack.TrackStreamInfo.PlayCount++;

                            await dbContext.StreamHistory.AddAsync(new StreamHistory
                            {
                                Played = now,
                                TrackStreamInfo = queuetrack.TrackStreamInfo
                            }, cancellationToken);

                            dbContext.StreamQueue.Remove(queuetrack);

                            await dbContext.StreamQueue.ForEachAsync(q => q.SortOrder--, cancellationToken);

                            await dbContext.SaveChangesAsync(cancellationToken);
                        }
                    }
                    finally
                    {
                        _queueMutex.Release();
                    }

                    if (queuetrack == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                    else
                    {
                        var artists = string.Empty;

                        string[] trackPeople = queuetrack.TrackStreamInfo.Track.TrackPersonGroupPersonRelations
                            .Where(g => g.PersonGroup.PersonGroupStreamInfo is { IncludeInAutoPlaylist: true })
                            .SelectMany(g => g.Persons)
                            .Distinct()
                            .Select(p => (p.FirstName + " " + p.LastName).Trim())
                            .ToArray();

                        if (trackPeople.Length > 0)
                        {
                            artists = string.Join(", ", trackPeople);
                        }
                        else
                        {
                            string[] albumPeople = queuetrack.TrackStreamInfo.Track.Disc.Album.AlbumPersonGroupPersonRelations
                                .Where(g => g.PersonGroup.PersonGroupStreamInfo is { IncludeInAutoPlaylist: true })
                                .SelectMany(g => g.Persons)
                                .Distinct()
                                .Select(p => (p.FirstName + " " + p.LastName).Trim())
                                .ToArray();

                            if (albumPeople.Length > 0)
                            {
                                artists = string.Join(", ", albumPeople);
                            }
                        }

                        Track trackToPlay = new()
                        {
                            File = queuetrack.TrackStreamInfo.FilePath,
                            Album = queuetrack.TrackStreamInfo.Track.Disc.Album.Title,
                            Title = queuetrack.TrackStreamInfo.Track.Title,
                            Artist = artists,
                            Duration = queuetrack.TrackStreamInfo.Track.Duration
                        };

                        await _cambion.PublishEventAsync(new PlayTrack(trackToPlay));

                        await Task.Delay(queuetrack.TrackStreamInfo.Track.Duration, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Task is cancelled. Ignore this and just pass it on to TPL.
                _log.LogDebug("{method} is stopped", nameof(CurrentlyPlayingTask));
                throw;
            }
            catch (Exception e)
            {
                _log.LogError(e, "Unknown exception during {method}. Task is stopped.", nameof(CurrentlyPlayingTask));
            }
        }

        public void HandleEvent(PlayNextTrack input)
        {
            // Stop playlist task before starting a new instance
            _currentlyPlayingTaskCancellationTokenSource?.Cancel();

            _currentlyPlayingTaskCancellationTokenSource = new CancellationTokenSource();

            _ = CurrentlyPlayingTask(_currentlyPlayingTaskCancellationTokenSource.Token);
        }
    }
}
