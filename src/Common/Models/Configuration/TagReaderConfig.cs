using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Common.Models.Configuration
{
    public class TagReaderConfig
    {
        public const string Section = "TagReader";

        public bool AlbumTitleArticleNormalization { get; set; }
        public string[] NormalizationArticles { get; set; }
        public Dictionary<string, int> TagMappings { get; set; }
    }
}
