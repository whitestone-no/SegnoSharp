using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(IncludeInAutoPlaylist))]
    public class TrackStreamInfo
    {
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; }
        public bool IncludeInAutoPlaylist { get; set; }
        public int Weight { get; set; } = 100;

        public int TrackId { get; set; }
        public Track Track { get; set; }

        [DeleteBehavior(DeleteBehavior.Cascade)]
        public IList<StreamQueue> StreamQueue { get; set; }

        [DeleteBehavior(DeleteBehavior.Cascade)]
        public IList<StreamHistory> StreamHistory { get; set; }
    }
}
