using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ActioNator.Data.Migrations
{
    /// <inheritdoc />
    public partial class AchievementTemplateSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(9003),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 575, DateTimeKind.Utc).AddTicks(9508));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(175),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 575, DateTimeKind.Utc).AddTicks(2724));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 716, DateTimeKind.Utc).AddTicks(4972),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 571, DateTimeKind.Utc).AddTicks(9224));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 715, DateTimeKind.Utc).AddTicks(7899),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 571, DateTimeKind.Utc).AddTicks(2578));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 714, DateTimeKind.Utc).AddTicks(5949),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 570, DateTimeKind.Utc).AddTicks(1586));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 713, DateTimeKind.Utc).AddTicks(7887),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 569, DateTimeKind.Utc).AddTicks(3951));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 710, DateTimeKind.Utc).AddTicks(3285),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 566, DateTimeKind.Utc).AddTicks(2117));

            migrationBuilder.InsertData(
                table: "AchievementTemplates",
                columns: new[] { "Id", "Description", "Title" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "You've successfully joined ActioNator!", "Welcome Aboard" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "You've set your first personal goal.", "First Goal Set" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "You completed your first workout.", "First Workout Logged" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "You've logged activity for 7 consecutive days.", "Consistency Is Key" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), "You wrote your first journal entry.", "Journal Initiate" },
                    { new Guid("00000000-0000-0000-0000-000000000006"), "You've created your first post.", "Community Voice" },
                    { new Guid("00000000-0000-0000-0000-000000000007"), "You've commented on 5 posts or journal entries.", "Supportive Soul" },
                    { new Guid("00000000-0000-0000-0000-000000000008"), "You've successfully completed 10 goals.", "10 Goals Completed" },
                    { new Guid("00000000-0000-0000-0000-000000000009"), "You've logged 50 workouts. Amazing consistency!", "50 Workouts Strong" },
                    { new Guid("00000000-0000-0000-0000-000000000010"), "You have been verified as a certified coach.", "Coach Verified" },
                    { new Guid("00000000-0000-0000-0000-000000000011"), "Completed a workout before 7 AM.", "Early Riser" },
                    { new Guid("00000000-0000-0000-0000-000000000012"), "Completed 100 workouts.", "Workout Hero" },
                    { new Guid("00000000-0000-0000-0000-000000000013"), "Wrote 10 consecutive journal entries.", "Mindful Moment" },
                    { new Guid("00000000-0000-0000-0000-000000000014"), "Connected with 5 new users.", "Social Butterfly" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "AchievementTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000014"));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 575, DateTimeKind.Utc).AddTicks(9508),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(9003));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 575, DateTimeKind.Utc).AddTicks(2724),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 721, DateTimeKind.Utc).AddTicks(175));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 571, DateTimeKind.Utc).AddTicks(9224),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 716, DateTimeKind.Utc).AddTicks(4972));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 571, DateTimeKind.Utc).AddTicks(2578),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 715, DateTimeKind.Utc).AddTicks(7899));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 570, DateTimeKind.Utc).AddTicks(1586),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 714, DateTimeKind.Utc).AddTicks(5949));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 569, DateTimeKind.Utc).AddTicks(3951),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 713, DateTimeKind.Utc).AddTicks(7887));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 7, 11, 8, 23, 39, 566, DateTimeKind.Utc).AddTicks(2117),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 7, 11, 8, 59, 37, 710, DateTimeKind.Utc).AddTicks(3285));
        }
    }
}
