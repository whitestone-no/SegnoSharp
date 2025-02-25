using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;
using Whitestone.SegnoSharp.Common.Models;

namespace Whitestone.SegnoSharp.Modules.StreamControls.Models
{
    public class Settings : StreamingSettings
    {
        [Persist]
        [DefaultValue(nameof(AudioFormat.Mp3))]
        public override AudioFormat AudioFormat { get; set; }

        [Persist]
        [DefaultValue(nameof(Bitrate.Kbps128))]
        public override Bitrate Bitrate { get; set; }

        [Persist]
        [DefaultValue("localhost")]
        public override string Hostname { get; set; }

        [Persist]
        [DefaultValue(8000)]
        public override int Port { get; set; }
        
        [Persist]
        [DefaultValue("/stream")]
        public override string MountPoint { get; set; }

        [Persist]
        [DefaultValue("hackme")]
        public override string Password { get; set; }

        [Persist]
        [DefaultValue(false)]
        public override bool IsPublic { get; set; }

        [Persist]
        [DefaultValue("SegnoSharp")]
        public override string Name { get; set; }

        [Persist]
        [DefaultValue("localhost")]
        public override string ServerUrl { get; set; }

        [Persist]
        [DefaultValue("Music")]
        public override string Genre { get; set; }

        [Persist]
        [DefaultValue("SegnoSharp")]
        public override string Description { get; set; }

        public bool IsStreaming { get; set; }
    }
}
