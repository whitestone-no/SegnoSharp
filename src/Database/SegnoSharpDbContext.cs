﻿using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Database
{
    public class SegnoSharpDbContext(DbContextOptions<SegnoSharpDbContext> options, IConfiguration configuration)
        : DbContext(options)
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
        public DbSet<PersonGroupStreamInfo> PersonGroupsStreamInfos { get; set; }
        public DbSet<AlbumPersonGroupPersonRelation> AlbumPersonGroupsRelations { get; set; }
        public DbSet<TrackPersonGroupPersonRelation> TrackPersonGroupsRelations { get; set; }
        public DbSet<TrackGroup> TrackGroups { get; set; }
        public DbSet<AlbumCover> AlbumCovers { get; set; }
        public DbSet<AlbumCoverData> AlbumCoversData { get; set; }
        public DbSet<StreamQueue> StreamQueue { get; set; }
        public DbSet<StreamHistory> StreamHistory { get; set; }

        public DbSet<PersistenceManagerEntry> PersistenceManagerEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            string databaseType = configuration.GetSection("Database").GetChildren().FirstOrDefault(c => c.Key == "Type")?.Value?.ToLower();

            switch (databaseType)
            {
                case "sqlite":
                    modelBuilder.UseCollation("NOCASE");
                    modelBuilder.Entity<Album>().Property(t => t.Title).HasColumnType("TEXT COLLATE NOCASE");
                    modelBuilder.Entity<Genre>().Property(g => g.Name).HasColumnType("TEXT COLLATE NOCASE");
                    modelBuilder.Entity<RecordLabel>().Property(g => g.Name).HasColumnType("TEXT COLLATE NOCASE");
                    modelBuilder.Entity<Person>().Property(g => g.FirstName).HasColumnType("TEXT COLLATE NOCASE");
                    modelBuilder.Entity<Person>().Property(g => g.LastName).HasColumnType("TEXT COLLATE NOCASE");
                    modelBuilder.Entity<Track>().Property(g => g.Title).HasColumnType("TEXT COLLATE NOCASE");
                    break;
                case "mysql":
                    modelBuilder.UseCollation("utf8mb4_unicode_ci");
                    break;
                case "postgresql":
                    break;
                case "mssql":
                    modelBuilder.UseCollation("Latin1_General_CI_AI");
                    break;
                default:
                    throw new ArgumentException($"Unsupported database type: {databaseType}");
            }

            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 1, Type = PersonGroupType.Album, Name = "Artist", SortOrder = 1 });
            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 2, Type = PersonGroupType.Track, Name = "Artist", SortOrder = 1 });
            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 3, Type = PersonGroupType.Album, Name = "Composer", SortOrder = 2 });
            modelBuilder.Entity<PersonGroup>().HasData(new PersonGroup { Id = 4, Type = PersonGroupType.Track, Name = "Composer", SortOrder = 2 });

            modelBuilder.Entity<PersonGroupStreamInfo>().HasData(new PersonGroupStreamInfo { Id = 1, IncludeInAutoPlaylist = true, PersonGroupId = 1 });
            modelBuilder.Entity<PersonGroupStreamInfo>().HasData(new PersonGroupStreamInfo { Id = 2, IncludeInAutoPlaylist = true, PersonGroupId = 2 });

            modelBuilder.Entity<MediaType>().HasData(new MediaType { Id = 1, Name = "CD", SortOrder = 1 });
            modelBuilder.Entity<MediaType>().HasData(new MediaType { Id = 2, Name = "DVD-Audio", SortOrder = 2 });
            modelBuilder.Entity<MediaType>().HasData(new MediaType { Id = 3, Name = "Super Audio CD", SortOrder = 3 });
            modelBuilder.Entity<MediaType>().HasData(new MediaType { Id = 4, Name = "Digital Download", SortOrder = 4 });

            // Don't allow empty strings in nullable text types.
            modelBuilder.Entity<Album>().Property(e => e.Upc).HasConversion(s => string.IsNullOrWhiteSpace(s) ? null : s, s => s);
            modelBuilder.Entity<Album>().Property(e => e.CatalogueNumber).HasConversion(s => string.IsNullOrWhiteSpace(s) ? null : s, s => s);
            modelBuilder.Entity<Disc>().Property(e => e.Title).HasConversion(s => string.IsNullOrWhiteSpace(s) ? null : s, s => s);
            modelBuilder.Entity<Track>().Property(e => e.Notes).HasConversion(s => string.IsNullOrWhiteSpace(s) ? null : s, s => s);
            modelBuilder.Entity<Person>().Property(e => e.FirstName).HasConversion(s => string.IsNullOrWhiteSpace(s) ? null : s, s => s);

            base.OnModelCreating(modelBuilder);
        }
    }
}
