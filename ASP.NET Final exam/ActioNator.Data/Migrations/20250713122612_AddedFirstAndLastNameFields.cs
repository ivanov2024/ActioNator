using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActioNator.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedFirstAndLastNameFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 854, DateTimeKind.Utc).AddTicks(4502),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(9003));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 854, DateTimeKind.Utc).AddTicks(171),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(175));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 852, DateTimeKind.Utc).AddTicks(1035),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 716, DateTimeKind.Utc).AddTicks(4972));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 851, DateTimeKind.Utc).AddTicks(7368),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 715, DateTimeKind.Utc).AddTicks(7899));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 851, DateTimeKind.Utc).AddTicks(1393),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 714, DateTimeKind.Utc).AddTicks(5949));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 850, DateTimeKind.Utc).AddTicks(7475),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 713, DateTimeKind.Utc).AddTicks(7887));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 848, DateTimeKind.Utc).AddTicks(6319),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 710, DateTimeKind.Utc).AddTicks(3285));

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(9003),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 854, DateTimeKind.Utc).AddTicks(4502));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(175),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 854, DateTimeKind.Utc).AddTicks(171));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 716, DateTimeKind.Utc).AddTicks(4972),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 852, DateTimeKind.Utc).AddTicks(1035));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 715, DateTimeKind.Utc).AddTicks(7899),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 851, DateTimeKind.Utc).AddTicks(7368));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 714, DateTimeKind.Utc).AddTicks(5949),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 851, DateTimeKind.Utc).AddTicks(1393));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 713, DateTimeKind.Utc).AddTicks(7887),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 850, DateTimeKind.Utc).AddTicks(7475));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 710, DateTimeKind.Utc).AddTicks(3285),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 13, 12, 26, 8, 848, DateTimeKind.Utc).AddTicks(6319));
        }
    }
}
