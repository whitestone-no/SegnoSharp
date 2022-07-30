using System;
using System.Collections.Generic;
using System.IO;
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
                File = "music/Lord of the Rings, The; The Return of the King (The Complete Recordings) - Howard Shore/CD3/06 - The Mûmakil.flac"
            },
            new Track
            {
                Album = "Doctor Who - Series 5",
                Artist = "Murray Gold",
                Title = "Doctor Who XI",
                File = "music/Doctor Who - Series 5 - Murray Gold/CD1/01 - Doctor Who XI.flac"
            },
            new Track
            {
                Album = "Chicken Run",
                Artist = "Harry Gregson-Williams & John Powell",
                Title = "Chickens Are Not Organized",
                File = "music/Chicken Run - Harry Gregson-Williams & John Powell/05 - Chickens Are Not Organized.flac"
            },
            new Track
            {
                Album = "Avengers: Age Of Ultron",
                Artist = "Bryan Tyler",
                Title = "Avengers: Age Of Ultron Title",
                File = "music/Avengers; Age Of Ultron - Bryan Tyler & Danny Elfman/01 - Avengers; Age Of Ultron Title.flac"
            },
        };

        private readonly ITagReader _tagReader;
        private readonly CommonConfig _commonConfig;
        private readonly Random _randomizer = new Random();

        public PlaylistHandler(ITagReader tagReader, IOptions<CommonConfig> commonConfig)
        {
            _tagReader = tagReader;
            _commonConfig = commonConfig.Value;
        }

        public Track GetNextTrack()
        {
            Track track;
            do
            {
                int nextIndex = _randomizer.Next(0, 4);

                track = _tracks[nextIndex];
                track.File = Path.Combine(_commonConfig.DataPath, track.File);
            } while (!File.Exists(track.File));

            return track;
        }
    }
}
