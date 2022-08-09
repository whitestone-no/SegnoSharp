using Microsoft.EntityFrameworkCore;

namespace Whitestone.WASP.Database
{
    public class WaspDbContext : DbContext
    {
        public WaspDbContext(DbContextOptions options) : base(options)
        {
        }
    }

    public class WaspMysqlDbContext : WaspDbContext
    {
        public WaspMysqlDbContext(DbContextOptions options) : base(options)
        {
        }
    }

    public class WaspSqliteDbContext : WaspDbContext
    {
        public WaspSqliteDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}
