using System.ComponentModel.DataAnnotations;

namespace Whitestone.WASP.Database.Models
{
    public class PersonGroup
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public ushort SortOrder { get; set; }
        public PersonGroupType Type { get; set; }

        public ICollection<AlbumPersonGroupPersonRelation> AlbumPersonGroupPersonRelations { get; set; }
        public ICollection<TrackPersonGroupPersonRelation> TrackPersonGroupPersonRelations { get; set; }
    }

    public enum PersonGroupType
    {
        Album,
        Track
    }
}
