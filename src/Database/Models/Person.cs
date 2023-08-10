using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(LastName), nameof(FirstName))]
    public class Person
    {
        public int Id { get; set; }

        [Required]
        public string LastName { get; set; }
        public string FirstName { get; set; }

        public ushort Version { get; set; }

        public ICollection<AlbumPersonGroupPersonRelation> AlbumPersonGroupPersonRelations { get; set; }
        public ICollection<TrackPersonGroupPersonRelation> TrackPersonGroupPersonRelations { get; set; }
    }
}
