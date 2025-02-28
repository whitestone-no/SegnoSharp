using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Common.Models.Persistent
{
    public class StreamingSettings
    {
        public virtual AudioFormat AudioFormat { get; set; }
        public virtual Bitrate Bitrate { get; set; }
        public virtual string Hostname { get; set; }
        public virtual ushort Port { get; set; }
        public virtual string MountPoint { get; set; }
        public virtual string Password { get; set; }
        public virtual bool IsPublic { get; set; }
        public virtual string Name { get; set; }
        public virtual string ServerUrl { get; set; }
        public virtual string Genre { get; set; }
        public virtual string Description { get; set; }
    }

    public enum Bitrate
    {
        [FriendlyName("64 kbps")]
        Kbps64 = 64,
        [FriendlyName("128 kbps")]
        Kbps128 = 128,
        [FriendlyName("192 kbps")]
        Kbps192 = 192,
        [FriendlyName("256 kbps")]
        Kbps256 = 256,
        [FriendlyName("320 kbps")]
        Kbps320 = 320
    }

    public enum AudioFormat
    {
        [FriendlyName("MP3")]
        Mp3,
        [FriendlyName("AAC")]
        Aac
    }

}
