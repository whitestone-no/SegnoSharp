using System;
using System.Linq;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.BassService.Interfaces;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Common.Models.Configuration;

namespace Whitestone.SegnoSharp.BassService.Helpers
{
    public class TagReader : ITagReader
    {
        private readonly IBassWrapper _bassWrapper;
        private readonly TagReaderConfig _config;

        public TagReader(IBassWrapper bassWrapper, IOptions<TagReaderConfig> config)
        {
            _bassWrapper = bassWrapper;
            _config = config.Value;
        }

        public Tags ReadTagInfo(string file)
        {
            Tags tags = _bassWrapper.GetTagFromFile(file);
            tags.File = file;

            if (!_config.AlbumTitleArticleNormalization)
            {
                return tags;
            }

            if (!_config.NormalizationArticles.Any(a => tags.Album.StartsWith(a + " ")))
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
    }
}
