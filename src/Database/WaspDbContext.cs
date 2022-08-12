using Microsoft.EntityFrameworkCore;
using Whitestone.WASP.Database.Models;

namespace Whitestone.WASP.Database
{
    public class WaspDbContext : DbContext
    {
        public DbSet<Album> Albums { get; set; }
        public DbSet<Disc> Discs { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<RecordLabel> RecordLabels { get; set; }
        public DbSet<MediaType> MediaTypes { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<TrackStreamInfo> TrackStreamInfos { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<PersonGroup> PersonGroups { get; set; }
        public DbSet<AlbumPersonGroupPersonRelation> AlbumPersonGroupsRelations { get; set; }
        public DbSet<TrackPersonGroupPersonRelation> TrackPersonGroupsRelations { get; set; }
        public DbSet<TrackGroup> TrackGroups { get; set; }
        public DbSet<AlbumCover> AlbumCovers { get; set; }
        public DbSet<AlbumCoverData> AlbumCoversData { get; set; }
        public DbSet<StreamQueue> StreamQueue { get; set; }
        public DbSet<StreamHistory> StreamHistory { get; set; }

        public WaspDbContext( DbContextOptions options) : base(options)
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
