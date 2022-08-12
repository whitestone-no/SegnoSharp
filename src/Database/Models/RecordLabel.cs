using Microsoft.EntityFrameworkCore;

namespace Whitestone.WASP.Database.Models
{
    [Index(nameof(Name))]
    public class RecordLabel
    {
        public int Id { get; set; }
        public int Name { get; set; }

        public ICollection<Album> Albums { get; set; }
    }
}
