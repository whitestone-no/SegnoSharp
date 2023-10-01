using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Database
{
    public class SegnoSharpDbContext : DbContext
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

        public SegnoSharpDbContext( DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 1, Type = PersonGroupType.Album, Name = "Artist", SortOrder = 1 });
            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 2, Type = PersonGroupType.Track, Name = "Artist", SortOrder = 1 });
            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 3, Type = PersonGroupType.Track, Name = "Composer", SortOrder = 2 });

            base.OnModelCreating(modelBuilder);
        }
    }

    public class SegnoSharpMysqlDbContext : SegnoSharpDbContext
    {
        public SegnoSharpMysqlDbContext(DbContextOptions options) : base(options)
        {
        }
    }

    public class SegnoSharpSqliteDbContext : SegnoSharpDbContext
    {
        public SegnoSharpSqliteDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}
