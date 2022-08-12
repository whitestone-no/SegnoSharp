using System.ComponentModel.DataAnnotations;

namespace Whitestone.WASP.Database.Models
{
    public class AlbumCover
    {
        public int Id { get; set; }

        [Required]
        public string Filename { get; set; }
        [Required]
        public uint Filesize { get; set; }
        [Required]
        public string Mime { get; set; }

        public int AlbumId { get; set; }
        public Album Album { get; set; }
        public AlbumCoverData AlbumCoverData { get; set; }
    }
}
