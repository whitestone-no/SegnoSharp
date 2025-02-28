using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Common.Models.Configuration
{
    public class TagReaderConfig
    {
        public const string Section = "TagReader";

        // TODO: Should this be moved to MediaImporter?
        public Dictionary<string, int> TagMappings { get; set; }
    }
}
