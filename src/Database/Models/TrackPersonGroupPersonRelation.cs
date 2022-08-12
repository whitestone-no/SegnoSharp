using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Whitestone.WASP.Database.Models
{
    public class TrackPersonGroupPersonRelation
    {
        public int Id { get; set; }

        [Required]
        public Track Track { get; set; }
        [Required]
        public PersonGroup PersonGroup { get; set; }
        [Required]
        public ICollection<Person> Persons { get; set; }
    }
}
