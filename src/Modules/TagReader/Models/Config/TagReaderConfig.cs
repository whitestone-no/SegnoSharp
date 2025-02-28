using Whitestone.SegnoSharp.Common.Models.Configuration;

namespace Whitestone.SegnoSharp.Modules.TagReader.Models.Config
{
    public class TagReaderConfigExt : TagReaderConfig
    {
        public bool AlbumTitleArticleNormalization { get; set; }
        public string[] NormalizationArticles { get; set; }
    }
}
