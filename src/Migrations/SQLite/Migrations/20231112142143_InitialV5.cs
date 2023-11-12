using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Whitestone.SegnoSharp.Database.Migrations.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class InitialV5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackGroups_Discs_DiscId",
                table: "TrackGroups");

            migrationBuilder.AlterColumn<int>(
                name: "DiscId",
                table: "TrackGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackGroups_Discs_DiscId",
                table: "TrackGroups",
                column: "DiscId",
                principalTable: "Discs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackGroups_Discs_DiscId",
                table: "TrackGroups");

            migrationBuilder.AlterColumn<int>(
                name: "DiscId",
                table: "TrackGroups",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_TrackGroups_Discs_DiscId",
                table: "TrackGroups",
                column: "DiscId",
                principalTable: "Discs",
                principalColumn: "Id");
        }
    }
}
