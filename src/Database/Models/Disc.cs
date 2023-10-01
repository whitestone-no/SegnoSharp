using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Whitestone.SegnoSharp.Database.Models
{
    public class Disc
    {
        public int Id { get; set; }
        [Required]
        public byte DiscNumber { get; set; }
        public string Title { get; set; }

        public Album Album { get; set; }
        public IList<MediaType> MediaTypes { get; set; }
        public IList<Track> Tracks { get; set; }
        public IList<TrackGroup> TrackGroups { get; set; }
    }
}
