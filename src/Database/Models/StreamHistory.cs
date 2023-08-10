namespace Whitestone.SegnoSharp.Database.Models
{
    public class StreamHistory
    {
        public uint Id { get; set; }
        public DateTime Played { get; set; }

        public TrackStreamInfo TrackStreamInfo { get; set; }
    }
}
