using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(Name))]
    public class RecordLabel
    {
        public int Id { get; set; }
        public int Name { get; set; }

        public IList<Album> Albums { get; set; }
    }
}
