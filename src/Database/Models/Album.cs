using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(Title))]
    public class Album
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public ushort Published { get; set; }
        public string Upc { get; set; }
        public string CatalogueNumber { get; set; }
        public DateTime Added { get; set; }
        public bool IsPublic { get; set; }

        public AlbumCover AlbumCover { get; set; }
        public ICollection<Disc> Discs { get; set; }
        public ICollection<Genre> Genres { get; set; }
        public ICollection<RecordLabel> RecordLabels { get; set; }
        public ICollection<AlbumPersonGroupPersonRelation> AlbumPersonGroupPersonRelations { get; set; }
        public ICollection<TrackPersonGroupPersonRelation> TrackPersonGroupPersonRelations { get; set; }
    }
}
