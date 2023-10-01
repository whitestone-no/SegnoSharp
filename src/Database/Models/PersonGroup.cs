using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Whitestone.SegnoSharp.Database.Models
{
    public class PersonGroup
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public ushort SortOrder { get; set; }
        public PersonGroupType Type { get; set; }

        public IList<AlbumPersonGroupPersonRelation> AlbumPersonGroupPersonRelations { get; set; }
        public IList<TrackPersonGroupPersonRelation> TrackPersonGroupPersonRelations { get; set; }
    }

    public enum PersonGroupType
    {
        Album,
        Track
    }
}
