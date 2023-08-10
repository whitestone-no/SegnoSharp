namespace Whitestone.SegnoSharp.Database.Models
{
    public class StreamQueue
    {
        public uint Id { get; set; }
        public ushort SortOrder { get; set; }

        public TrackStreamInfo TrackStreamInfo { get; set; }
    }
}
