using System.ComponentModel;
using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Shared.Models.Persistent
{
    public class StreamingSettings : INotifyPropertyChanged
    {
        [Persist]
        [Attributes.PersistenceManager.DefaultValue(nameof(AudioFormat.Mp3))]
        [Attributes.PersistenceManager.Description("Audio format")]
        public AudioFormat AudioFormat { get; set; }

        [Persist]
        [Attributes.PersistenceManager.DefaultValue(nameof(Bitrate.Kbps128))]
        public Bitrate Bitrate { get; set; }

        [Persist]
        [Attributes.PersistenceManager.DefaultValue(nameof(ServerType.Icecast))]
        public ServerType ServerType { get; set; }

        [Persist]
        [Attributes.PersistenceManager.DefaultValue("localhost")]
        public string Hostname { get; set; }
        
        [Persist]
        [Attributes.PersistenceManager.DefaultValue(8000)]
        public ushort Port { get; set; }
        
        [Persist]
        [Attributes.PersistenceManager.DefaultValue("/stream")]
        [Attributes.PersistenceManager.Description("Mount point")]
        public string MountPoint { get; set; }

        [Persist]
        [Attributes.PersistenceManager.DefaultValue("hackme")]
        public string Password { get; set; }

        [Persist]
        [Attributes.PersistenceManager.DefaultValue(false)]
        [Attributes.PersistenceManager.Description("Is public")]
        public bool IsPublic { get; set; }

        [Persist]
        [Attributes.PersistenceManager.DefaultValue("SegnoSharp")]
        public string Name { get; set; }
        
        [Persist]
        [Attributes.PersistenceManager.DefaultValue("localhost")]
        [Attributes.PersistenceManager.Description("Server URL")]
        public string ServerUrl { get; set; }

        [Persist]
        [Attributes.PersistenceManager.DefaultValue("Music")]
        public string Genre { get; set; }

        [Persist]
        [Attributes.PersistenceManager.DefaultValue("SegnoSharp")]
        public string Description { get; set; }

        [Persist]
        [Attributes.PersistenceManager.DefaultValue("%title% - %artist% (%album%)")]
        [Attributes.PersistenceManager.Description("Title sent to streaming server. Use a combination of %album%, %title%, and %artist%")]
        public string TitleFormat { get; set; }

        [Persist]
        [Attributes.PersistenceManager.DefaultValue(false)]
        [Attributes.PersistenceManager.Description("Connect and start streaming to server on startup")]
        public bool StartStreamOnStartup { get; set; }

        private bool _isStreaming;
        public bool IsStreaming
        {
            get => _isStreaming;
            set
            {
                if (_isStreaming == value)
                {
                    return;
                }

                _isStreaming = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStreaming)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
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

    public enum ServerType
    {
        [FriendlyName("Icecast")]
        Icecast,
        [FriendlyName("Shoutcast")]
        Shoutcast
    }
}
