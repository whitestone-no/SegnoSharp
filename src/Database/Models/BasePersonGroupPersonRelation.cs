using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Whitestone.SegnoSharp.Database.Models
{
    public abstract class BasePersonGroupPersonRelation { }

    public abstract class BasePersonGroupPersonRelation<TParent> : BasePersonGroupPersonRelation 
    {
        public int Id { get; set; }

        [Required]
        public TParent Parent { get; set; }
        [Required]
        public PersonGroup PersonGroup { get; set; }
        [Required]
        public IList<Person> Persons { get; set; }
    }
}
