using Microsoft.AspNetCore.HttpOverrides;

namespace Whitestone.SegnoSharp.Shared.Models.Configuration
{
    public class SiteConfig
    {
        public const string Section = "SiteConfig";

        public string DataPath { get; set; }
        public string MusicPath { get; set; }
        public string SharedSecret { get; set; }
        public bool BehindProxy { get; set; }

        private string _basePath;
        public string BasePath
        {
            get => _basePath;
            set
            {
                if (string.IsNullOrEmpty(value) || value == "/")
                {
                    _basePath = "/";
                    return;
                }
                string tempPath = value.Trim('/');
                _basePath = "/" + tempPath + "/";
            }
        }
    }
}
