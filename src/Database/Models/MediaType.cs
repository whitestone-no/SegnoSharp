using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Whitestone.SegnoSharp.Database.Interfaces;

namespace Whitestone.SegnoSharp.Database.Models
{
    public class MediaType : ITag
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public byte SortOrder { get; set; }

        public IList<Disc> Discs { get; set; }

        [NotMapped]
        public string TagName
        {
            get => Name;
            set => Name = value;
        }
    }
}
