using Microsoft.AspNetCore.HttpOverrides;

namespace Whitestone.SegnoSharp.Shared.Models.Configuration
{
    public class SiteConfig
    {
        public const string Section = "SiteConfig";

        public string DataPath { get; set; }
        public string MusicPath { get; set; }
        public string SharedSecret { get; set; }
        public string ProxyNetwork { get; set; }
    }
}
