using System.ComponentModel.DataAnnotations;

namespace Whitestone.WASP.Database.Models
{
    public class MediaType
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public byte SortOrder { get; set; }

        public ICollection<Disc> Discs { get; set; }
    }
}
