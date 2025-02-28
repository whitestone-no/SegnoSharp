namespace Whitestone.SegnoSharp.Modules.TagReader.Models.Config
{
    public class TagReaderConfig
    {
        public const string Section = "TagReader";

        public bool AlbumTitleArticleNormalization { get; set; }
        public string[] NormalizationArticles { get; set; }
    }
}
