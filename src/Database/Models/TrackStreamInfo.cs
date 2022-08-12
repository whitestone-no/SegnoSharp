using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.WASP.Database.Models
{
    [Index(nameof(IncludeInAutoPlaylist), nameof(LastPlayed), nameof(PlayCount))]
    public class TrackStreamInfo
    {
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; }
        public bool IncludeInAutoPlaylist { get; set; }
        public DateTime? LastPlayed { get; set; }
        public int PlayCount { get; set; }

        public int TrackId { get; set; }
        public Track Track { get; set; }
        public ICollection<StreamQueue> StreamQueue { get; set; }
        public ICollection<StreamHistory> StreamHistory { get; set; }
    }
}
