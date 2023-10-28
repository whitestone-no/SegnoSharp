using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        private readonly IConfiguration _configuration;

        public SegnoSharpDbContext(DbContextOptions<SegnoSharpDbContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            string databaseType = _configuration.GetSection("Database").GetChildren().FirstOrDefault(c => c.Key == "Type")?.Value?.ToLower();

            switch (databaseType)
            {
                case "sqlite":
                    modelBuilder.UseCollation("NOCASE");
                    modelBuilder.Entity<Album>().Property(t => t.Title).HasColumnType("TEXT COLLATE NOCASE");
                    modelBuilder.Entity<Genre>().Property(g => g.Name).HasColumnType("TEXT COLLATE NOCASE");
                    break;
                case "mysql":
                    modelBuilder.UseCollation("utf8mb4_unicode_ci");
                    break;
                default:
                    throw new ArgumentException($"Unsupported database type: {databaseType}");
            }

            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 1, Type = PersonGroupType.Album, Name = "Artist", SortOrder = 1 });
            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 2, Type = PersonGroupType.Track, Name = "Artist", SortOrder = 1 });
            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 3, Type = PersonGroupType.Album, Name = "Composer", SortOrder = 2 });
            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 4, Type = PersonGroupType.Track, Name = "Composer", SortOrder = 2 });

            modelBuilder.Entity<MediaType>().HasData(new MediaType { Id = 1, Name = "CD", SortOrder = 1 });
            modelBuilder.Entity<MediaType>().HasData(new MediaType { Id = 2, Name = "DVD-Audio", SortOrder = 2 });
            modelBuilder.Entity<MediaType>().HasData(new MediaType { Id = 3, Name = "Super Audio CD", SortOrder = 3 });
            modelBuilder.Entity<MediaType>().HasData(new MediaType { Id = 4, Name = "Digital Download", SortOrder = 4 });

            base.OnModelCreating(modelBuilder);
        }
    }
}
