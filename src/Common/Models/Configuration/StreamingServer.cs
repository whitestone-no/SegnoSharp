namespace Whitestone.WASP.Common.Models.Configuration
{
    public class StreamingServer
    {
        public const string Section = "StreamingServer";

        public string Address { get; set; }
        public int Port { get; set; }
        public string AdminPassword { get; set; }
        public string AdminUsername { get; set; }
        public bool IsPublic { get; set; }
        public string MountPoint { get; set; }
        public string Password { get; set; }
        public string Genre { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

    }
}
