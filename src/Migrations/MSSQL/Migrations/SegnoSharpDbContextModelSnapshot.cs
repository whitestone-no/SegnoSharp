﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Whitestone.SegnoSharp.Database;

#nullable disable

namespace Whitestone.SegnoSharp.Database.Migrations.MSSQL.Migrations
{
    [DbContext(typeof(SegnoSharpDbContext))]
    partial class SegnoSharpDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseCollation("Latin1_General_CI_AI")
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("AlbumGenre", b =>
                {
                    b.Property<int>("AlbumsId")
                        .HasColumnType("int");

                    b.Property<int>("GenresId")
                        .HasColumnType("int");

                    b.HasKey("AlbumsId", "GenresId");

                    b.HasIndex("GenresId");

                    b.ToTable("AlbumGenre");
                });

            modelBuilder.Entity("AlbumPersonGroupPersonRelationPerson", b =>
                {
                    b.Property<int>("AlbumPersonGroupPersonRelationsId")
                        .HasColumnType("int");

                    b.Property<int>("PersonsId")
                        .HasColumnType("int");

                    b.HasKey("AlbumPersonGroupPersonRelationsId", "PersonsId");

                    b.HasIndex("PersonsId");

                    b.ToTable("AlbumPersonGroupPersonRelationPerson");
                });

            modelBuilder.Entity("AlbumRecordLabel", b =>
                {
                    b.Property<int>("AlbumsId")
                        .HasColumnType("int");

                    b.Property<int>("RecordLabelsId")
                        .HasColumnType("int");

                    b.HasKey("AlbumsId", "RecordLabelsId");

                    b.HasIndex("RecordLabelsId");

                    b.ToTable("AlbumRecordLabel");
                });

            modelBuilder.Entity("DiscMediaType", b =>
                {
                    b.Property<int>("DiscsId")
                        .HasColumnType("int");

                    b.Property<int>("MediaTypesId")
                        .HasColumnType("int");

                    b.HasKey("DiscsId", "MediaTypesId");

                    b.HasIndex("MediaTypesId");

                    b.ToTable("DiscMediaType");
                });

            modelBuilder.Entity("PersonTrackPersonGroupPersonRelation", b =>
                {
                    b.Property<int>("PersonsId")
                        .HasColumnType("int");

                    b.Property<int>("TrackPersonGroupPersonRelationsId")
                        .HasColumnType("int");

                    b.HasKey("PersonsId", "TrackPersonGroupPersonRelationsId");

                    b.HasIndex("TrackPersonGroupPersonRelationsId");

                    b.ToTable("PersonTrackPersonGroupPersonRelation");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.Album", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Added")
                        .HasColumnType("datetime2");

                    b.Property<string>("CatalogueNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsPublic")
                        .HasColumnType("bit");

                    b.Property<int>("Published")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Upc")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Title");

                    b.ToTable("Albums");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.AlbumCover", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AlbumId")
                        .HasColumnType("int");

                    b.Property<string>("Filename")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("Filesize")
                        .HasColumnType("bigint");

                    b.Property<string>("Mime")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AlbumId")
                        .IsUnique();

                    b.ToTable("AlbumCovers");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.AlbumCoverData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AlbumCoverId")
                        .HasColumnType("int");

                    b.Property<byte[]>("Data")
                        .HasColumnType("varbinary(max)");

                    b.HasKey("Id");

                    b.HasIndex("AlbumCoverId")
                        .IsUnique();

                    b.ToTable("AlbumCoversData");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.AlbumPersonGroupPersonRelation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ParentId")
                        .HasColumnType("int");

                    b.Property<int>("PersonGroupId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("PersonGroupId");

                    b.ToTable("AlbumPersonGroupsRelations");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.Disc", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AlbumId")
                        .HasColumnType("int");

                    b.Property<byte>("DiscNumber")
                        .HasColumnType("tinyint");

                    b.Property<string>("Title")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AlbumId");

                    b.ToTable("Discs");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.Genre", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.ToTable("Genres");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.MediaType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte>("SortOrder")
                        .HasColumnType("tinyint");

                    b.HasKey("Id");

                    b.ToTable("MediaTypes");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "CD",
                            SortOrder = (byte)1
                        },
                        new
                        {
                            Id = 2,
                            Name = "DVD-Audio",
                            SortOrder = (byte)2
                        },
                        new
                        {
                            Id = 3,
                            Name = "Super Audio CD",
                            SortOrder = (byte)3
                        },
                        new
                        {
                            Id = 4,
                            Name = "Digital Download",
                            SortOrder = (byte)4
                        });
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.PersistenceManagerEntry", b =>
                {
                    b.Property<string>("Key")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("DataType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Key");

                    b.ToTable("PersistenceManagerEntries");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.Person", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("FirstName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("LastName", "FirstName");

                    b.ToTable("Persons");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.PersonGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("PersonGroups");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Artist",
                            SortOrder = 1,
                            Type = 0
                        },
                        new
                        {
                            Id = 2,
                            Name = "Artist",
                            SortOrder = 1,
                            Type = 1
                        },
                        new
                        {
                            Id = 3,
                            Name = "Composer",
                            SortOrder = 2,
                            Type = 0
                        },
                        new
                        {
                            Id = 4,
                            Name = "Composer",
                            SortOrder = 2,
                            Type = 1
                        });
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.PersonGroupStreamInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IncludeInAutoPlaylist")
                        .HasColumnType("bit");

                    b.Property<int>("PersonGroupId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("IncludeInAutoPlaylist");

                    b.HasIndex("PersonGroupId")
                        .IsUnique();

                    b.ToTable("PersonGroupsStreamInfos");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            IncludeInAutoPlaylist = true,
                            PersonGroupId = 1
                        },
                        new
                        {
                            Id = 2,
                            IncludeInAutoPlaylist = true,
                            PersonGroupId = 2
                        });
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.RecordLabel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.ToTable("RecordLabels");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.StreamHistory", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("Played")
                        .HasColumnType("datetime2");

                    b.Property<int?>("TrackStreamInfoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("TrackStreamInfoId");

                    b.ToTable("StreamHistory");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.StreamQueue", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<int?>("TrackStreamInfoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("TrackStreamInfoId");

                    b.ToTable("StreamQueue");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.Track", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("DiscId")
                        .HasColumnType("int");

                    b.Property<int>("Length")
                        .HasColumnType("int");

                    b.Property<string>("Notes")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("TrackNumber")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("DiscId");

                    b.HasIndex("Title");

                    b.ToTable("Tracks");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.TrackGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("DiscId")
                        .HasColumnType("int");

                    b.Property<int>("GroupBeforeTrackNumber")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("DiscId");

                    b.ToTable("TrackGroups");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.TrackPersonGroupPersonRelation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ParentId")
                        .HasColumnType("int");

                    b.Property<int>("PersonGroupId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("PersonGroupId");

                    b.ToTable("TrackPersonGroupsRelations");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.TrackStreamInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IncludeInAutoPlaylist")
                        .HasColumnType("bit");

                    b.Property<int>("TrackId")
                        .HasColumnType("int");

                    b.Property<int>("Weight")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("IncludeInAutoPlaylist");

                    b.HasIndex("TrackId")
                        .IsUnique();

                    b.ToTable("TrackStreamInfos");
                });

            modelBuilder.Entity("AlbumGenre", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Album", null)
                        .WithMany()
                        .HasForeignKey("AlbumsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Genre", null)
                        .WithMany()
                        .HasForeignKey("GenresId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AlbumPersonGroupPersonRelationPerson", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.AlbumPersonGroupPersonRelation", null)
                        .WithMany()
                        .HasForeignKey("AlbumPersonGroupPersonRelationsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Person", null)
                        .WithMany()
                        .HasForeignKey("PersonsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AlbumRecordLabel", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Album", null)
                        .WithMany()
                        .HasForeignKey("AlbumsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Whitestone.SegnoSharp.Database.Models.RecordLabel", null)
                        .WithMany()
                        .HasForeignKey("RecordLabelsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("DiscMediaType", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Disc", null)
                        .WithMany()
                        .HasForeignKey("DiscsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Whitestone.SegnoSharp.Database.Models.MediaType", null)
                        .WithMany()
                        .HasForeignKey("MediaTypesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PersonTrackPersonGroupPersonRelation", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Person", null)
                        .WithMany()
                        .HasForeignKey("PersonsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Whitestone.SegnoSharp.Database.Models.TrackPersonGroupPersonRelation", null)
                        .WithMany()
                        .HasForeignKey("TrackPersonGroupPersonRelationsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.AlbumCover", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Album", "Album")
                        .WithOne("AlbumCover")
                        .HasForeignKey("Whitestone.SegnoSharp.Database.Models.AlbumCover", "AlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Album");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.AlbumCoverData", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.AlbumCover", "AlbumCover")
                        .WithOne("AlbumCoverData")
                        .HasForeignKey("Whitestone.SegnoSharp.Database.Models.AlbumCoverData", "AlbumCoverId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AlbumCover");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.AlbumPersonGroupPersonRelation", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Album", "Parent")
                        .WithMany("AlbumPersonGroupPersonRelations")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Whitestone.SegnoSharp.Database.Models.PersonGroup", "PersonGroup")
                        .WithMany("AlbumPersonGroupPersonRelations")
                        .HasForeignKey("PersonGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Parent");

                    b.Navigation("PersonGroup");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.Disc", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Album", "Album")
                        .WithMany("Discs")
                        .HasForeignKey("AlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Album");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.PersonGroupStreamInfo", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.PersonGroup", "PersonGroup")
                        .WithOne("PersonGroupStreamInfo")
                        .HasForeignKey("Whitestone.SegnoSharp.Database.Models.PersonGroupStreamInfo", "PersonGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PersonGroup");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.StreamHistory", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.TrackStreamInfo", "TrackStreamInfo")
                        .WithMany("StreamHistory")
                        .HasForeignKey("TrackStreamInfoId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("TrackStreamInfo");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.StreamQueue", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.TrackStreamInfo", "TrackStreamInfo")
                        .WithMany("StreamQueue")
                        .HasForeignKey("TrackStreamInfoId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("TrackStreamInfo");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.Track", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Disc", "Disc")
                        .WithMany("Tracks")
                        .HasForeignKey("DiscId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Disc");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.TrackGroup", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Disc", "Disc")
                        .WithMany("TrackGroups")
                        .HasForeignKey("DiscId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Disc");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.TrackPersonGroupPersonRelation", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Track", "Parent")
                        .WithMany("TrackPersonGroupPersonRelations")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Whitestone.SegnoSharp.Database.Models.PersonGroup", "PersonGroup")
                        .WithMany("TrackPersonGroupPersonRelations")
                        .HasForeignKey("PersonGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Parent");

                    b.Navigation("PersonGroup");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.TrackStreamInfo", b =>
                {
                    b.HasOne("Whitestone.SegnoSharp.Database.Models.Track", "Track")
                        .WithOne("TrackStreamInfo")
                        .HasForeignKey("Whitestone.SegnoSharp.Database.Models.TrackStreamInfo", "TrackId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Track");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.Album", b =>
                {
                    b.Navigation("AlbumCover");

                    b.Navigation("AlbumPersonGroupPersonRelations");

                    b.Navigation("Discs");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.AlbumCover", b =>
                {
                    b.Navigation("AlbumCoverData");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.Disc", b =>
                {
                    b.Navigation("TrackGroups");

                    b.Navigation("Tracks");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.PersonGroup", b =>
                {
                    b.Navigation("AlbumPersonGroupPersonRelations");

                    b.Navigation("PersonGroupStreamInfo");

                    b.Navigation("TrackPersonGroupPersonRelations");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.Track", b =>
                {
                    b.Navigation("TrackPersonGroupPersonRelations");

                    b.Navigation("TrackStreamInfo");
                });

            modelBuilder.Entity("Whitestone.SegnoSharp.Database.Models.TrackStreamInfo", b =>
                {
                    b.Navigation("StreamHistory");

                    b.Navigation("StreamQueue");
                });
#pragma warning restore 612, 618
        }
    }
}
