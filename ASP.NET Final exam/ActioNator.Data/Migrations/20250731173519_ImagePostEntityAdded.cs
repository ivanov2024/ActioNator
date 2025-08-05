using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActioNator.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImagePostEntityAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 352, DateTimeKind.Utc).AddTicks(6662),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 489, DateTimeKind.Utc).AddTicks(1657));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 351, DateTimeKind.Utc).AddTicks(9318),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 488, DateTimeKind.Utc).AddTicks(2181));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 348, DateTimeKind.Utc).AddTicks(781),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 483, DateTimeKind.Utc).AddTicks(5353));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 346, DateTimeKind.Utc).AddTicks(9495),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 482, DateTimeKind.Utc).AddTicks(6840));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 345, DateTimeKind.Utc).AddTicks(7059),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 481, DateTimeKind.Utc).AddTicks(3867));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 344, DateTimeKind.Utc).AddTicks(9070),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 480, DateTimeKind.Utc).AddTicks(3881));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 340, DateTimeKind.Utc).AddTicks(7212),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 475, DateTimeKind.Utc).AddTicks(8554));

            migrationBuilder.CreateTable(
                name: "PostImage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostImage_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostImage_PostId",
                table: "PostImage",
                column: "PostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostImage");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 489, DateTimeKind.Utc).AddTicks(1657),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 352, DateTimeKind.Utc).AddTicks(6662));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 488, DateTimeKind.Utc).AddTicks(2181),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 351, DateTimeKind.Utc).AddTicks(9318));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 483, DateTimeKind.Utc).AddTicks(5353),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 348, DateTimeKind.Utc).AddTicks(781));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 482, DateTimeKind.Utc).AddTicks(6840),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 346, DateTimeKind.Utc).AddTicks(9495));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 481, DateTimeKind.Utc).AddTicks(3867),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 345, DateTimeKind.Utc).AddTicks(7059));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 480, DateTimeKind.Utc).AddTicks(3881),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 344, DateTimeKind.Utc).AddTicks(9070));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 27, 14, 40, 9, 475, DateTimeKind.Utc).AddTicks(8554),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 340, DateTimeKind.Utc).AddTicks(7212));
        }
    }
}
