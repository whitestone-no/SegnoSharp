using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(IsArtistCredit))]
    public class PersonGroupStreamInfo
    {
        public int Id { get; set; }

        /// <summary>
        /// Credits in this group say who a track or album is by. Used for the playlist page's
        /// artist line, the automatic playlist and its rule against repeating an artist, and
        /// by the agent tools to name a track's artist and tell same-titled tracks apart. Kept
        /// on the column it had as IncludeInAutoPlaylist, so the rename needs no migration.
        /// </summary>
        [Column("IncludeInAutoPlaylist")]
        public bool IsArtistCredit { get; set; }

        public int PersonGroupId { get; set; }
        public PersonGroup PersonGroup { get; set; }
    }
}