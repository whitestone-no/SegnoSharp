using System.ComponentModel.DataAnnotations;

namespace Whitestone.WASP.Database.Models
{
    public class Disc
    {
        public int Id { get; set; }
        [Required]
        public byte DiscNumber { get; set; }
        public string Title { get; set; }

        public Album Album { get; set; }
        public ICollection<MediaType> MediaTypes { get; set; }
        public ICollection<Track> Tracks { get; set; }
        public ICollection<TrackGroup> TrackGroups { get; set; }
    }
}
