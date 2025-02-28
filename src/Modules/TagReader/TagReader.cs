﻿using Microsoft.Extensions.Options;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Un4seen.Bass.AddOn.Tags;
using Un4seen.Bass;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Modules.TagReader.Interfaces;
using Whitestone.SegnoSharp.Modules.TagReader.Models.Config;

namespace Whitestone.SegnoSharp.Modules.TagReader
{
    internal class TagReader : ITagReader
    {
        private readonly IBassWrapper _bassWrapper;
        private readonly IOptions<TagReaderConfigExt> _config;
        private readonly ILogger<TagReader> _log;

        public TagReader(IBassWrapper bassWrapper, IOptions<TagReaderConfigExt> config, IOptions<BassRegistration> bassRegistration, ILogger<TagReader> log)
        {
            _bassWrapper = bassWrapper;
            _config = config;
            _log = log;

            _bassWrapper.Registration(bassRegistration.Value.Email, bassRegistration.Value.Key);
        }

        public Tags ReadTagInfo(string file)
        {
            Tags tags = ReadFromBass(file);
            tags.File = file;

            if (!_config.Value.AlbumTitleArticleNormalization)
            {
                return tags;
            }

            if (!_config.Value.NormalizationArticles.Any(a => tags.Album.StartsWith(a + " ")))
            {
                return tags;
            }

            int firstSpaceIndex = tags.Album.IndexOf(" ", StringComparison.OrdinalIgnoreCase);
            if (firstSpaceIndex == -1)
            {
                return tags;
            }

            string titleWithoutArticle = tags.Album.Substring(firstSpaceIndex + 1, tags.Album.Length - firstSpaceIndex - 1);
            string article = tags.Album.Substring(0, firstSpaceIndex);
            tags.Album = titleWithoutArticle + ", " + article;

            return tags;
        }

        private Tags ReadFromBass(string file)
        {
            int stream = _bassWrapper.CreateFileStream(file, 0, 0, (int)BASSFlag.BASS_DEFAULT);

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

            _log.LogWarning("Could not read tags from {file}", file);

            return null;
        }
    }
}
