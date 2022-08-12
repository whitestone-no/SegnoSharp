using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.WASP.Database.Models
{
    [Index(nameof(Name))]
    public class Genre
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        public ICollection<Album> Albums { get; set; }
    }
}
