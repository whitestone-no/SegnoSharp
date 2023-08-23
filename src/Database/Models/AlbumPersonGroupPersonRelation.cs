using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Whitestone.SegnoSharp.Database.Models
{
    public class AlbumPersonGroupPersonRelation
    {
        public int Id { get; set; }

        [Required]
        public Album Album { get; set; }
        [Required]
        public PersonGroup PersonGroup { get; set; }
        [Required]
        public ICollection<Person> Persons { get; set; }
    }
}
