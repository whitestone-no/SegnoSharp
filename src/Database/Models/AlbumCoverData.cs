namespace Whitestone.SegnoSharp.Database.Models
{
    public class AlbumCoverData
    {
        public int Id { get; set; }
        public byte[] Data { get; set; }

        public int AlbumCoverId { get; set; }
        public AlbumCover AlbumCover { get; set; }
    }
}
