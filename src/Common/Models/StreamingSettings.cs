namespace Whitestone.SegnoSharp.Common.Models
{
    public class StreamingSettings
    {
        public virtual AudioFormat AudioFormat { get; set; }
        public virtual Bitrate Bitrate { get; set; }
        public virtual string Hostname { get; set; }
        public virtual int Port { get; set; }
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
        Kbps64 = 64,
        Kbps128 = 128,
        Kbps192 = 192,
        Kbps256 = 256,
        Kbps320 = 320
    }

    public enum AudioFormat
    {
        Mp3,
        Aac
    }

}
