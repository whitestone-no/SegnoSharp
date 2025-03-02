using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Whitestone.SegnoSharp.Database.Migrations.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Albums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Published = table.Column<int>(type: "int", nullable: false),
                    Upc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CatalogueNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Added = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersistenceManagerEntries",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersistenceManagerEntries", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "PersonGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecordLabels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordLabels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlbumCovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Filename = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Filesize = table.Column<long>(type: "bigint", nullable: false),
                    Mime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlbumId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumCovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlbumCovers_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Discs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlbumId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Discs_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlbumGenre",
                columns: table => new
                {
                    AlbumsId = table.Column<int>(type: "int", nullable: false),
                    GenresId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumGenre", x => new { x.AlbumsId, x.GenresId });
                    table.ForeignKey(
                        name: "FK_AlbumGenre_Albums_AlbumsId",
                        column: x => x.AlbumsId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumGenre_Genres_GenresId",
                        column: x => x.GenresId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlbumPersonGroupsRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    PersonGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumPersonGroupsRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlbumPersonGroupsRelations_Albums_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumPersonGroupsRelations_PersonGroups_PersonGroupId",
                        column: x => x.PersonGroupId,
                        principalTable: "PersonGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonGroupsStreamInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncludeInAutoPlaylist = table.Column<bool>(type: "bit", nullable: false),
                    PersonGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonGroupsStreamInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonGroupsStreamInfos_PersonGroups_PersonGroupId",
                        column: x => x.PersonGroupId,
                        principalTable: "PersonGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlbumRecordLabel",
                columns: table => new
                {
                    AlbumsId = table.Column<int>(type: "int", nullable: false),
                    RecordLabelsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumRecordLabel", x => new { x.AlbumsId, x.RecordLabelsId });
                    table.ForeignKey(
                        name: "FK_AlbumRecordLabel_Albums_AlbumsId",
                        column: x => x.AlbumsId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumRecordLabel_RecordLabels_RecordLabelsId",
                        column: x => x.RecordLabelsId,
                        principalTable: "RecordLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlbumCoversData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    AlbumCoverId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumCoversData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlbumCoversData_AlbumCovers_AlbumCoverId",
                        column: x => x.AlbumCoverId,
                        principalTable: "AlbumCovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscMediaType",
                columns: table => new
                {
                    DiscsId = table.Column<int>(type: "int", nullable: false),
                    MediaTypesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscMediaType", x => new { x.DiscsId, x.MediaTypesId });
                    table.ForeignKey(
                        name: "FK_DiscMediaType_Discs_DiscsId",
                        column: x => x.DiscsId,
                        principalTable: "Discs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscMediaType_MediaTypes_MediaTypesId",
                        column: x => x.MediaTypesId,
                        principalTable: "MediaTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GroupBeforeTrackNumber = table.Column<int>(type: "int", nullable: false),
                    DiscId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackGroups_Discs_DiscId",
                        column: x => x.DiscId,
                        principalTable: "Discs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Length = table.Column<int>(type: "int", nullable: false),
                    DiscId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tracks_Discs_DiscId",
                        column: x => x.DiscId,
                        principalTable: "Discs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlbumPersonGroupPersonRelationPerson",
                columns: table => new
                {
                    AlbumPersonGroupPersonRelationsId = table.Column<int>(type: "int", nullable: false),
                    PersonsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumPersonGroupPersonRelationPerson", x => new { x.AlbumPersonGroupPersonRelationsId, x.PersonsId });
                    table.ForeignKey(
                        name: "FK_AlbumPersonGroupPersonRelationPerson_AlbumPersonGroupsRelations_AlbumPersonGroupPersonRelationsId",
                        column: x => x.AlbumPersonGroupPersonRelationsId,
                        principalTable: "AlbumPersonGroupsRelations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumPersonGroupPersonRelationPerson_Persons_PersonsId",
                        column: x => x.PersonsId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackPersonGroupsRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    PersonGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackPersonGroupsRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackPersonGroupsRelations_PersonGroups_PersonGroupId",
                        column: x => x.PersonGroupId,
                        principalTable: "PersonGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackPersonGroupsRelations_Tracks_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackStreamInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IncludeInAutoPlaylist = table.Column<bool>(type: "bit", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    TrackId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackStreamInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackStreamInfos_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonTrackPersonGroupPersonRelation",
                columns: table => new
                {
                    PersonsId = table.Column<int>(type: "int", nullable: false),
                    TrackPersonGroupPersonRelationsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonTrackPersonGroupPersonRelation", x => new { x.PersonsId, x.TrackPersonGroupPersonRelationsId });
                    table.ForeignKey(
                        name: "FK_PersonTrackPersonGroupPersonRelation_Persons_PersonsId",
                        column: x => x.PersonsId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonTrackPersonGroupPersonRelation_TrackPersonGroupsRelations_TrackPersonGroupPersonRelationsId",
                        column: x => x.TrackPersonGroupPersonRelationsId,
                        principalTable: "TrackPersonGroupsRelations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StreamHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Played = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrackStreamInfoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamHistory_TrackStreamInfos_TrackStreamInfoId",
                        column: x => x.TrackStreamInfoId,
                        principalTable: "TrackStreamInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StreamQueue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TrackStreamInfoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamQueue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamQueue_TrackStreamInfos_TrackStreamInfoId",
                        column: x => x.TrackStreamInfoId,
                        principalTable: "TrackStreamInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MediaTypes",
                columns: new[] { "Id", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "CD", (byte)1 },
                    { 2, "DVD-Audio", (byte)2 },
                    { 3, "Super Audio CD", (byte)3 },
                    { 4, "Digital Download", (byte)4 }
                });

            migrationBuilder.InsertData(
                table: "PersonGroups",
                columns: new[] { "Id", "Name", "SortOrder", "Type" },
                values: new object[,]
                {
                    { 1, "Artist", 1, 0 },
                    { 2, "Artist", 1, 1 },
                    { 3, "Composer", 2, 0 },
                    { 4, "Composer", 2, 1 }
                });

            migrationBuilder.InsertData(
                table: "PersonGroupsStreamInfos",
                columns: new[] { "Id", "IncludeInAutoPlaylist", "PersonGroupId" },
                values: new object[,]
                {
                    { 1, true, 1 },
                    { 2, true, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlbumCovers_AlbumId",
                table: "AlbumCovers",
                column: "AlbumId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlbumCoversData_AlbumCoverId",
                table: "AlbumCoversData",
                column: "AlbumCoverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlbumGenre_GenresId",
                table: "AlbumGenre",
                column: "GenresId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbumPersonGroupPersonRelationPerson_PersonsId",
                table: "AlbumPersonGroupPersonRelationPerson",
                column: "PersonsId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbumPersonGroupsRelations_ParentId",
                table: "AlbumPersonGroupsRelations",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbumPersonGroupsRelations_PersonGroupId",
                table: "AlbumPersonGroupsRelations",
                column: "PersonGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbumRecordLabel_RecordLabelsId",
                table: "AlbumRecordLabel",
                column: "RecordLabelsId");

            migrationBuilder.CreateIndex(
                name: "IX_Albums_Title",
                table: "Albums",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_DiscMediaType_MediaTypesId",
                table: "DiscMediaType",
                column: "MediaTypesId");

            migrationBuilder.CreateIndex(
                name: "IX_Discs_AlbumId",
                table: "Discs",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroupsStreamInfos_IncludeInAutoPlaylist",
                table: "PersonGroupsStreamInfos",
                column: "IncludeInAutoPlaylist");

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroupsStreamInfos_PersonGroupId",
                table: "PersonGroupsStreamInfos",
                column: "PersonGroupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_LastName_FirstName",
                table: "Persons",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonTrackPersonGroupPersonRelation_TrackPersonGroupPersonRelationsId",
                table: "PersonTrackPersonGroupPersonRelation",
                column: "TrackPersonGroupPersonRelationsId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordLabels_Name",
                table: "RecordLabels",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StreamHistory_TrackStreamInfoId",
                table: "StreamHistory",
                column: "TrackStreamInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamQueue_TrackStreamInfoId",
                table: "StreamQueue",
                column: "TrackStreamInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackGroups_DiscId",
                table: "TrackGroups",
                column: "DiscId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackPersonGroupsRelations_ParentId",
                table: "TrackPersonGroupsRelations",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackPersonGroupsRelations_PersonGroupId",
                table: "TrackPersonGroupsRelations",
                column: "PersonGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_DiscId",
                table: "Tracks",
                column: "DiscId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_Title",
                table: "Tracks",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_TrackStreamInfos_IncludeInAutoPlaylist",
                table: "TrackStreamInfos",
                column: "IncludeInAutoPlaylist");

            migrationBuilder.CreateIndex(
                name: "IX_TrackStreamInfos_TrackId",
                table: "TrackStreamInfos",
                column: "TrackId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlbumCoversData");

            migrationBuilder.DropTable(
                name: "AlbumGenre");

            migrationBuilder.DropTable(
                name: "AlbumPersonGroupPersonRelationPerson");

            migrationBuilder.DropTable(
                name: "AlbumRecordLabel");

            migrationBuilder.DropTable(
                name: "DiscMediaType");

            migrationBuilder.DropTable(
                name: "PersistenceManagerEntries");

            migrationBuilder.DropTable(
                name: "PersonGroupsStreamInfos");

            migrationBuilder.DropTable(
                name: "PersonTrackPersonGroupPersonRelation");

            migrationBuilder.DropTable(
                name: "StreamHistory");

            migrationBuilder.DropTable(
                name: "StreamQueue");

            migrationBuilder.DropTable(
                name: "TrackGroups");

            migrationBuilder.DropTable(
                name: "AlbumCovers");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "AlbumPersonGroupsRelations");

            migrationBuilder.DropTable(
                name: "RecordLabels");

            migrationBuilder.DropTable(
                name: "MediaTypes");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "TrackPersonGroupsRelations");

            migrationBuilder.DropTable(
                name: "TrackStreamInfos");

            migrationBuilder.DropTable(
                name: "PersonGroups");

            migrationBuilder.DropTable(
                name: "Tracks");

            migrationBuilder.DropTable(
                name: "Discs");

            migrationBuilder.DropTable(
                name: "Albums");
        }
    }
}
