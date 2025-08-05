using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActioNator.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedSeveralEntitiesByAddingFieldsAndNewEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Workouts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 535, DateTimeKind.Utc).AddTicks(2495),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 352, DateTimeKind.Utc).AddTicks(6662));

            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SharesCount",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 534, DateTimeKind.Utc).AddTicks(2775),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 351, DateTimeKind.Utc).AddTicks(9318));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 529, DateTimeKind.Utc).AddTicks(4082),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 348, DateTimeKind.Utc).AddTicks(781));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 527, DateTimeKind.Utc).AddTicks(9869),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 346, DateTimeKind.Utc).AddTicks(9495));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 526, DateTimeKind.Utc).AddTicks(1442),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 345, DateTimeKind.Utc).AddTicks(7059));

            migrationBuilder.AddColumn<bool>(
                name: "IsAuthor",
                table: "Comments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                table: "Comments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 525, DateTimeKind.Utc).AddTicks(2431),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 344, DateTimeKind.Utc).AddTicks(9070));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 520, DateTimeKind.Utc).AddTicks(4232),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 340, DateTimeKind.Utc).AddTicks(7212));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 520, DateTimeKind.Utc).AddTicks(4730));

            migrationBuilder.CreateTable(
                name: "UserLoginHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_UserId",
                table: "UserLoginHistories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLoginHistories");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Workouts");

            migrationBuilder.DropColumn(
                name: "LikesCount",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "SharesCount",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "IsAuthor",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "LikesCount",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 352, DateTimeKind.Utc).AddTicks(6662),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 535, DateTimeKind.Utc).AddTicks(2495));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 351, DateTimeKind.Utc).AddTicks(9318),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 534, DateTimeKind.Utc).AddTicks(2775));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 348, DateTimeKind.Utc).AddTicks(781),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 529, DateTimeKind.Utc).AddTicks(4082));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 346, DateTimeKind.Utc).AddTicks(9495),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 527, DateTimeKind.Utc).AddTicks(9869));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 345, DateTimeKind.Utc).AddTicks(7059),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 526, DateTimeKind.Utc).AddTicks(1442));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 344, DateTimeKind.Utc).AddTicks(9070),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 525, DateTimeKind.Utc).AddTicks(2431));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 31, 17, 35, 18, 340, DateTimeKind.Utc).AddTicks(7212),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 520, DateTimeKind.Utc).AddTicks(4232));
        }
    }
}
