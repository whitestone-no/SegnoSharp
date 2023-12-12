namespace Whitestone.SegnoSharp.Common.Models.Configuration
{
    public class CommonConfig
    {
        public const string Section = "CommonConfig";

        public string DataPath { get; set; }
        public string MusicPath { get; set; }
        public bool EnableDebugPage { get; set; }
    }
}
