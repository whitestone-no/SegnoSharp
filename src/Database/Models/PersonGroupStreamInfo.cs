using Microsoft.EntityFrameworkCore;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(IncludeInAutoPlaylist))]
    public class PersonGroupStreamInfo
    {
        public int Id { get; set; }

        public bool IncludeInAutoPlaylist { get; set; }

        public int PersonGroupId { get; set; }
        public PersonGroup PersonGroup { get; set; }
    }
}
