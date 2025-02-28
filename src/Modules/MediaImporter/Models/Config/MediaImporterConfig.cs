using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.MediaImporter.Models.Config
{
    internal class MediaImporterConfig
    {
        public const string Section = "MediaImporter";

        public Dictionary<string, int> TagMappings { get; set; }

    }
}
