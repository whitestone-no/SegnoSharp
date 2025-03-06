using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Modules.MediaImporter.Models.Persistent
{
    public class MediaImporterSettings
    {
        [Persist]
        [DefaultValue(true)]
        public bool NormalizeAlbumTitles { get; set; }

        [Persist]
        [DefaultValue("The,A,An")]
        public string NormalizationArticles { get; set; }
    }
}
