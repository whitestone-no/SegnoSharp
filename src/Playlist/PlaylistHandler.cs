using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whitestone.WASP.Common.Interfaces;
using Whitestone.WASP.Common.Models;
using Whitestone.WASP.Common.Models.Configuration;

namespace Whitestone.WASP.Playlist
{
    public class PlaylistHandler : IPlaylistHandler
    {
        private readonly List<Track> _tracks = new List<Track>
        {
            new Track
            {
                Album = "Lord of the Rings, The: The Return of the King (The Complete Recordings)",
                Artist = "Howard Shore",
                Title = "The Mûmakil",
                File = "Lord of the Rings, The; The Return of the King (The Complete Recordings) - Howard Shore/CD3/06 - The Mumakil.flac"
            },
            new Track
            {
                Album = "Doctor Who - Series 5",
                Artist = "Murray Gold",
                Title = "Doctor Who XI",
                File = "Doctor Who - Series 5 - Murray Gold/CD1/01 - Doctor Who XI.flac"
            },
            new Track
            {
                Album = "Chicken Run",
                Artist = "Harry Gregson-Williams & John Powell",
                Title = "Chickens Are Not Organized",
                File = "Chicken Run - Harry Gregson-Williams & John Powell/05 - Chickens Are Not Organized.flac"
            },
            new Track
            {
                Album = "Avengers: Age Of Ultron",
                Artist = "Bryan Tyler",
                Title = "Avengers: Age Of Ultron Title",
                File = "Avengers; Age Of Ultron - Bryan Tyler & Danny Elfman/01 - Avengers; Age Of Ultron Title.flac"
            }
        };

        private readonly ITagReader _tagReader;
        private readonly ILogger<PlaylistHandler> _log;
        private readonly CommonConfig _commonConfig;
        private readonly Random _randomizer = new Random();

        public PlaylistHandler(ITagReader tagReader, IOptions<CommonConfig> commonConfig, ILogger<PlaylistHandler> log)
        {
            _tagReader = tagReader;
            _log = log;
            _commonConfig = commonConfig.Value;
        }

        public Track GetNextTrack()
        {
            int noOfAttempts = 3;
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
    }
}
