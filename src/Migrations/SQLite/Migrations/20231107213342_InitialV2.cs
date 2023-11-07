using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Whitestone.SegnoSharp.Database.Migrations.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class InitialV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Discs_Albums_AlbumId",
                table: "Discs");

            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_Discs_DiscId",
                table: "Tracks");

            migrationBuilder.AlterColumn<int>(
                name: "DiscId",
                table: "Tracks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AlbumId",
                table: "Discs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Discs_Albums_AlbumId",
                table: "Discs",
                column: "AlbumId",
                principalTable: "Albums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_Discs_DiscId",
                table: "Tracks",
                column: "DiscId",
                principalTable: "Discs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Discs_Albums_AlbumId",
                table: "Discs");

            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_Discs_DiscId",
                table: "Tracks");

            migrationBuilder.AlterColumn<int>(
                name: "DiscId",
                table: "Tracks",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "AlbumId",
                table: "Discs",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Discs_Albums_AlbumId",
                table: "Discs",
                column: "AlbumId",
                principalTable: "Albums",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_Discs_DiscId",
                table: "Tracks",
                column: "DiscId",
                principalTable: "Discs",
                principalColumn: "Id");
        }
    }
}
