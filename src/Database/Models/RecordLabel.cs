using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database.Interfaces;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(Name))]
    public class RecordLabel : ITag
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        public IList<Album> Albums { get; set; }

        [NotMapped]
        public string TagName
        {
            get => Name;
            set => Name = value;
        }
    }
}
