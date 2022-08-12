using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.WASP.Database.Models
{
    [Index(nameof(Title))]
    public class Track
    {
        public int Id { get; set; }
        [Required]
        public ushort TrackNumber { get; set; }
        [Required]
        public string Title { get; set; }

        /// <summary>
        ///  Length of track, in seconds
        /// </summary>
        public ushort Length { get; set; }

        public Disc Disc { get; set; }
        public TrackStreamInfo TrackStreamInfo { get; set; }
    }
}
