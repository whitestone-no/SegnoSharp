using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(Title))]
    public class Track
    {
        public int Id { get; set; }
        [Required]
        public ushort TrackNumber { get; set; }
        [Required]
        public string Title { get; set; }
        public string Notes { get; set; }

        /// <summary>
        ///  Length of track, in seconds
        /// </summary>
        public ushort Length { get; set; }

        [NotMapped]
        public TimeSpan Duration
        {
            get => TimeSpan.FromSeconds(Length);
            set => Length = (ushort)value.TotalSeconds;
        }

        public int DiscId { get; set; }
        public Disc Disc { get; set; }
        public TrackStreamInfo TrackStreamInfo { get; set; }
        public IList<TrackPersonGroupPersonRelation> TrackPersonGroupPersonRelations { get; set; }
    }
}
