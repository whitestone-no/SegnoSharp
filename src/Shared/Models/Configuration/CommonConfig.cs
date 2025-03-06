namespace Whitestone.SegnoSharp.Shared.Models.Configuration
{
    public class CommonConfig
    {
        public const string Section = "CommonConfig";

        public string DataPath { get; set; }
        public string MusicPath { get; set; }
        public string SharedSecret { get; set; }
    }
}
