using Microsoft.EntityFrameworkCore;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(SecurityApiClientId), nameof(Permission), IsUnique = true)]
    public class SecurityApiClientPermission
    {
        public int Id { get; set; }
        public int SecurityApiClientId { get; set; }
        public string Permission { get; set; }

        public SecurityApiClient Client { get; set; }
    }
}
