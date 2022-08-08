using Microsoft.EntityFrameworkCore;

namespace Whitestone.WASP.Database
{
    public class WaspDbContext : DbContext
    {
        public WaspDbContext(DbContextOptions<WaspDbContext> options) : base(options)
        {
        }
    }
}
