using System.ComponentModel.DataAnnotations;

namespace Whitestone.SegnoSharp.Database.Models
{
    public class TrackGroup
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        public ushort GroupBeforeTrackNumber { get; set; }

        public int DiscId { get; set; }
        public Disc Disc { get; set; }
    }
}
