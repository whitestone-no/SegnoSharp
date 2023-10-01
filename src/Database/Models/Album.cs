using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
        public IList<Disc> Discs { get; set; }
        public IList<Genre> Genres { get; set; }
        public IList<RecordLabel> RecordLabels { get; set; }
        public IList<AlbumPersonGroupPersonRelation> AlbumPersonGroupPersonRelations { get; set; }
    }
}
