using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Un4seen.Bass.AddOn.Tags;
using Un4seen.Bass;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models;
using Whitestone.SegnoSharp.Modules.TagReader.Interfaces;
using Whitestone.SegnoSharp.Modules.TagReader.Models.Config;

namespace Whitestone.SegnoSharp.Modules.TagReader
{
    internal class TagReader : ITagReader
    {
        private readonly IBassWrapper _bassWrapper;
        private readonly ILogger<TagReader> _log;

        public TagReader(IBassWrapper bassWrapper, IOptions<BassRegistration> bassRegistration, ILogger<TagReader> log)
        {
            _bassWrapper = bassWrapper;
            _log = log;

            _bassWrapper.Registration(bassRegistration.Value.Email, bassRegistration.Value.Key);
        }

        public Tags ReadTagInfo(string file)
        {
            try
            {
                if (string.IsNullOrEmpty(file))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(file));
                }

                Tags tags = ReadFromBass(file);
                tags.File = file;

                return tags;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error reading tags from {file}", file);
                return null;
            }
        }

        private Tags ReadFromBass(string file)
        {
            int stream = _bassWrapper.CreateFileStream(file, 0, 0, (int)BASSFlag.BASS_DEFAULT);

            if (stream == 0)
            {
                _log.LogError("Could not create stream from {file}: {bassError}", file, _bassWrapper.GetLastBassError());
                return null;
            }

            TAG_INFO tags = new(file);
            if (_bassWrapper.GetTagsFromFile(stream, tags))
            {
                _ = ushort.TryParse(tags.year, out ushort year);
                _ = byte.TryParse(tags.disc, out byte disc);
                _ = ushort.TryParse(tags.track, out ushort trackNo);

                Tags tagsFromFile = new()
                {
                    Album = tags.album,
                    AlbumArtist = tags.albumartist,
                    Artist = tags.artist,
                    Composer = tags.composer,
                    Disc = disc,
                    Duration = tags.duration,
                    Genre = tags.genre,
                    Title = tags.title,
                    TrackNumber = trackNo,
                    Year = year,
                    Notes = tags.comment
                };

                if (tags.PictureCount <= 0)
                {
                    return tagsFromFile;
                }

                TagPicture tagPicture = tags.PictureGet(0);
                if (tagPicture is { PictureStorage: TagPicture.PICTURE_STORAGE.Internal, PictureType: TagPicture.PICTURE_TYPE.FrontAlbumCover })
                {
                    tagsFromFile.CoverImage = new TagsImage
                    {
                        MimeType = tagPicture.MIMEType,
                        Data = tagPicture.Data
                    };
                }

                return tagsFromFile;
            }

            _log.LogWarning("Could not read tags from {file}: {bassError}", file, _bassWrapper.GetLastBassError());

            return null;
        }
    }
}
