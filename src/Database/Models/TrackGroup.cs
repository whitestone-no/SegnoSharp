using System.ComponentModel.DataAnnotations;

namespace Whitestone.WASP.Database.Models
{
    public class TrackGroup
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        public ushort GroupBeforeTrackNumber { get; set; }

        public Disc Disc { get; set; }
    }
}
