using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActioNator.Data.Migrations
{
    /// <inheritdoc />
    public partial class MadeProfilePictureUrlNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 276, DateTimeKind.Utc).AddTicks(9587),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(9003));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 276, DateTimeKind.Utc).AddTicks(1568),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(175));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 271, DateTimeKind.Utc).AddTicks(5404),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 716, DateTimeKind.Utc).AddTicks(4972));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 270, DateTimeKind.Utc).AddTicks(2556),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 715, DateTimeKind.Utc).AddTicks(7899));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 268, DateTimeKind.Utc).AddTicks(5861),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 714, DateTimeKind.Utc).AddTicks(5949));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 267, DateTimeKind.Utc).AddTicks(7822),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 713, DateTimeKind.Utc).AddTicks(7887));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 264, DateTimeKind.Utc).AddTicks(610),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 710, DateTimeKind.Utc).AddTicks(3285));

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePictureUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(9003),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 276, DateTimeKind.Utc).AddTicks(9587));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(175),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 276, DateTimeKind.Utc).AddTicks(1568));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 716, DateTimeKind.Utc).AddTicks(4972),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 271, DateTimeKind.Utc).AddTicks(5404));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 715, DateTimeKind.Utc).AddTicks(7899),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 270, DateTimeKind.Utc).AddTicks(2556));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 714, DateTimeKind.Utc).AddTicks(5949),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 268, DateTimeKind.Utc).AddTicks(5861));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 713, DateTimeKind.Utc).AddTicks(7887),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 267, DateTimeKind.Utc).AddTicks(7822));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 710, DateTimeKind.Utc).AddTicks(3285),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 15, 13, 56, 20, 264, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePictureUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
