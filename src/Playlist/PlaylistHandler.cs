using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whitestone.Cambion.Interfaces;
using Whitestone.WASP.Common.Events;
using Whitestone.WASP.Common.Interfaces;
using Whitestone.WASP.Common.Models;
using Whitestone.WASP.Common.Models.Configuration;

namespace Whitestone.WASP.Playlist
{
    public class PlaylistHandler : IPlaylistHandler, IEventHandler<PlayerReady>, IEventHandler<PlayNextTrack>
    {
        private readonly List<Track> _tracks = new List<Track>
        {
            new Track
            {
                Album = "Lord of the Rings, The: The Return of the King (The Complete Recordings)",
                Artist = "Howard Shore",
                Title = "The Mûmakil",
                File = "Lord of the Rings, The; The Return of the King (The Complete Recordings) - Howard Shore/CD3/06 - The Mumakil.flac",
                Duration = TimeSpan.FromSeconds(57)
            },
            new Track
            {
                Album = "Doctor Who - Series 5",
                Artist = "Murray Gold",
                Title = "Doctor Who XI",
                File = "Doctor Who - Series 5 - Murray Gold/CD1/01 - Doctor Who XI.flac",
                Duration = TimeSpan.FromSeconds(64)
            },
            new Track
            {
                Album = "Chicken Run",
                Artist = "Harry Gregson-Williams & John Powell",
                Title = "Chickens Are Not Organized",
                File = "Chicken Run - Harry Gregson-Williams & John Powell/05 - Chickens Are Not Organized.flac",
                Duration = TimeSpan.FromSeconds(61)
            },
            new Track
            {
                Album = "Avengers: Age Of Ultron",
                Artist = "Bryan Tyler",
                Title = "Avengers: Age Of Ultron Title",
                File = "Avengers; Age Of Ultron - Bryan Tyler & Danny Elfman/01 - Avengers; Age Of Ultron Title.flac",
                Duration = TimeSpan.FromSeconds(43)
            }
        };

        private readonly ITagReader _tagReader;
        private readonly ICambion _cambion;
        private readonly ILogger<PlaylistHandler> _log;
        private readonly CommonConfig _commonConfig;
        private readonly Random _randomizer = new Random();
        private CancellationTokenSource _playlistTaskCancellationTokenSource;

        public PlaylistHandler(ITagReader tagReader, IOptions<CommonConfig> commonConfig, ICambion cambion, ILogger<PlaylistHandler> log)
        {
            _tagReader = tagReader;
            _cambion = cambion;
            _log = log;
            _commonConfig = commonConfig.Value;

            cambion.Register(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // No need to do anything specific during startup
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop the existing track countdown timers
            _playlistTaskCancellationTokenSource?.Cancel();

            return Task.CompletedTask;
        }

        public Track GetNextTrack(CancellationToken cancellationToken = default)
        {
            const int noOfAttempts = 3;

            IEnumerable<string> files = Directory.EnumerateFiles(_commonConfig.MusicPath, "*.flac", SearchOption.AllDirectories);
            int noOfFiles = files.Count();
            int fileAttempt = 0;

            do
            {
                fileAttempt++;
                int fileIndex = _randomizer.Next(0, noOfFiles);
                string file = files.ElementAt(fileIndex);

                _log.LogDebug("Next file to be played is {file}", file);

                bool fileExists = File.Exists(file);

                if (!fileExists)
                {
                    _log.LogError("File {file} does not exist. Getting another track from playlist.", file);
                    continue;
                }
                
                Tags tags = _tagReader.ReadTagInfo(file);
                if (tags == null)
                {
                    _log.LogError("Can't read tags from File {file}. Getting another track from playlist.", file);
                    continue;
                }

                Track fileTrack = new Track
                {
                    Album = tags.Album,
                    Artist = tags.Artist,
                    Title = tags.Title,
                    File = file,
                    Duration = TimeSpan.FromSeconds(tags.Duration)
                };

                return fileTrack;

            } while (fileAttempt <= noOfAttempts);

            _log.LogWarning("Could not find file in recursive file enumeration. Falling back to hardcoded list.");

            int attempt = 0;

            do
            {
                int nextIndex = _randomizer.Next(0, _tracks.Count);

                Track track = _tracks[nextIndex];
                track.File = Path.Combine(_commonConfig.MusicPath, track.File);

                bool fileExists = File.Exists(track.File);

                if (fileExists)
                {
                    return track;
                }

                _log.LogError("File {file} does not exist. Getting another track from playlist.", track.File);

                attempt++;

            } while (attempt < noOfAttempts);

            return null;
        }

        public void HandleEvent(PlayerReady input)
        {
            // Start timer that counts down from track.Duration and sends a new track to the player when current track reaches zero.
            // Do not await it as this should run in the background.

            if (_playlistTaskCancellationTokenSource != null)
            {
                _log.LogWarning("{event} was fired while PlaylistTask was already running. Doing nothing.", nameof(PlayerReady));
                return;
            }

            _playlistTaskCancellationTokenSource = new CancellationTokenSource();

            _ = PlaylistTask(_playlistTaskCancellationTokenSource.Token);
        }

        public async Task PlaylistTask(CancellationToken cancellationToken)
        {
            try
            {
                _log.LogDebug("{method} is started", nameof(PlaylistTask));

                while (!cancellationToken.IsCancellationRequested)
                {
                    Track track = GetNextTrack(cancellationToken);

                    await _cambion.PublishEventAsync(new PlayTrack(track));

                    await Task.Delay(track.Duration, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Task is cancelled. Ignore this and just pass it on to TPL.
                _log.LogDebug("{method} is stopped", nameof(PlaylistTask));
                throw;
            }
            catch (Exception e)
            {
                _log.LogError(e, "Unknown exception during {method}. Task is stopped.", nameof(PlaylistTask));
            }
        }

        public void HandleEvent(PlayNextTrack input)
        {
            if (_playlistTaskCancellationTokenSource != null)
            {
                // Playlist task is already running. Stop it before starting a new instance.
                _playlistTaskCancellationTokenSource.Cancel();
            }

            _playlistTaskCancellationTokenSource = new CancellationTokenSource();

            _ = PlaylistTask(_playlistTaskCancellationTokenSource.Token);
        }
    }
}
