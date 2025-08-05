using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ActioNator.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExerciseTemplateInitialSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 502, DateTimeKind.Utc).AddTicks(2952),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 535, DateTimeKind.Utc).AddTicks(2495));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 501, DateTimeKind.Utc).AddTicks(4003),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 534, DateTimeKind.Utc).AddTicks(2775));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 497, DateTimeKind.Utc).AddTicks(207),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 529, DateTimeKind.Utc).AddTicks(4082));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 495, DateTimeKind.Utc).AddTicks(7320),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 527, DateTimeKind.Utc).AddTicks(9869));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 494, DateTimeKind.Utc).AddTicks(4857),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 526, DateTimeKind.Utc).AddTicks(1442));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 493, DateTimeKind.Utc).AddTicks(5458),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 525, DateTimeKind.Utc).AddTicks(2431));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 488, DateTimeKind.Utc).AddTicks(9358),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 520, DateTimeKind.Utc).AddTicks(4232));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 488, DateTimeKind.Utc).AddTicks(9847),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 520, DateTimeKind.Utc).AddTicks(4730));

            migrationBuilder.InsertData(
                table: "ExerciseTemplates",
                columns: new[] { "Id", "ImageUrl", "Name", "TargetedMuscle" },
                values: new object[,]
                {
                    { new Guid("11be10a7-ec4d-4455-8eab-ce777739991a"), "~/images/exercise/gluteBridgeImage.webp", "Glute Bridge", "Glutes" },
                    { new Guid("1ab97f57-8902-4edb-9394-64e41d925d0b"), "~/images/exercise/highKneesImage.webp", "High Knees", "Cardio" },
                    { new Guid("1b10fe06-748e-437f-8190-7dcc80d4223b"), "~/images/exercise/tricepDipImage.png", "Tricep Dip", "Triceps" },
                    { new Guid("32fc5f9f-bb15-4868-a119-76ec4a6893ed"), "~/images/exercise/bicepCurlImage.webp", "Bicep Curl", "Biceps" },
                    { new Guid("35c5743e-9854-4e6d-ac9a-959965176fcd"), "~/images/exercise/deadliftImage.png", "Deadlift", "Back" },
                    { new Guid("3f3ab4f1-15e5-4d1d-a954-87b6c2ea50c7"), "~/images/exercise/toeTouchesImage.webp", "Toe Touches", "Hamstrings" },
                    { new Guid("3f9df2e6-a13e-40a4-bd08-485fb04f45bd"), "~/images/exercise/legRaisesImage.webp", "Leg Raises", "Lower Abs" },
                    { new Guid("465f2a36-546b-4087-bda8-538d7fcfc11a"), "~/images/exercise/pushUpImage.png", "Push Up", "Chest" },
                    { new Guid("652febc1-3742-48b0-b356-12444b88ff5b"), "~/images/exercise/mountainClimbersImage.png", "Mountain Climbers", "Full Body" },
                    { new Guid("7f2b1df2-4470-4af6-9b62-6e5024ae1194"), "~/images/exercise/sidePlankImage.png", "Side Plank", "Obliques" },
                    { new Guid("86c4d55e-f577-4380-9485-152c40d89a92"), "~/images/exercise/burpeesImage.webp", "Burpees", "Full Body" },
                    { new Guid("887e3155-a526-4381-a673-2d4ac1825575"), "~/images/exercise/shoulderPressImage.webp", "Shoulder Press", "Shoulders" },
                    { new Guid("91c9b0bc-0c1d-48e8-adcf-1909e958df86"), "~/images/exercise/pullUpImage.png", "Pull Up", "Back" },
                    { new Guid("97f56f43-4218-4c56-ae52-8d2e0ea31178"), "~/images/exercise/wallSitImage.png", "Wall Sit", "Quadriceps" },
                    { new Guid("9b2e0dc9-9f7b-4b65-b8db-d14f2b6a23cb"), "~/images/exercise/lungesImage.webp", "Lunges", "Legs" },
                    { new Guid("9d117e5e-204f-4223-843d-d103ec71dba4"), "~/images/exercise/jumpingJacksImage.png", "Jumping Jacks", "Cardio" },
                    { new Guid("a55a054d-453c-4fbd-bbdd-463116ee7e86"), "~/images/exercise/bentOverRowImage.webp", "Bent Over Row", "Upper Back" },
                    { new Guid("a8f0c913-c562-4796-9c65-70c95a38f17a"), "~/images/exercise/calfRaiseImage.jpeg", "Calf Raise", "Calves" },
                    { new Guid("b11cd880-a989-4da7-b5c3-5b3c68cd3a68"), "~/images/exercise/russianTwistsImage.webp", "Russian Twists", "Obliques" },
                    { new Guid("cbbe5588-6435-4692-9bef-fd3958b2f943"), "~/images/exercise/plankImage.png", "Plank", "Core" },
                    { new Guid("e4efbfad-aad5-42d6-815f-2dd612c7743e"), "~/images/exercise/squatImage.webp", "Squat", "Legs" },
                    { new Guid("ee6e57e2-df3e-4906-88bd-103880c39bcb"), "~/images/exercise/chestFlyImage.webp", "Chest Fly", "Chest" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11be10a7-ec4d-4455-8eab-ce777739991a"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("1ab97f57-8902-4edb-9394-64e41d925d0b"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("1b10fe06-748e-437f-8190-7dcc80d4223b"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("32fc5f9f-bb15-4868-a119-76ec4a6893ed"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("35c5743e-9854-4e6d-ac9a-959965176fcd"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("3f3ab4f1-15e5-4d1d-a954-87b6c2ea50c7"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("3f9df2e6-a13e-40a4-bd08-485fb04f45bd"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("465f2a36-546b-4087-bda8-538d7fcfc11a"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("652febc1-3742-48b0-b356-12444b88ff5b"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("7f2b1df2-4470-4af6-9b62-6e5024ae1194"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("86c4d55e-f577-4380-9485-152c40d89a92"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("887e3155-a526-4381-a673-2d4ac1825575"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("91c9b0bc-0c1d-48e8-adcf-1909e958df86"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("97f56f43-4218-4c56-ae52-8d2e0ea31178"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("9b2e0dc9-9f7b-4b65-b8db-d14f2b6a23cb"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("9d117e5e-204f-4223-843d-d103ec71dba4"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("a55a054d-453c-4fbd-bbdd-463116ee7e86"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("a8f0c913-c562-4796-9c65-70c95a38f17a"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("b11cd880-a989-4da7-b5c3-5b3c68cd3a68"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("cbbe5588-6435-4692-9bef-fd3958b2f943"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("e4efbfad-aad5-42d6-815f-2dd612c7743e"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("ee6e57e2-df3e-4906-88bd-103880c39bcb"));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 535, DateTimeKind.Utc).AddTicks(2495),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 502, DateTimeKind.Utc).AddTicks(2952));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 534, DateTimeKind.Utc).AddTicks(2775),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 501, DateTimeKind.Utc).AddTicks(4003));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 529, DateTimeKind.Utc).AddTicks(4082),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 497, DateTimeKind.Utc).AddTicks(207));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 527, DateTimeKind.Utc).AddTicks(9869),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 495, DateTimeKind.Utc).AddTicks(7320));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 526, DateTimeKind.Utc).AddTicks(1442),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 494, DateTimeKind.Utc).AddTicks(4857));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 525, DateTimeKind.Utc).AddTicks(2431),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 493, DateTimeKind.Utc).AddTicks(5458));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 520, DateTimeKind.Utc).AddTicks(4232),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 488, DateTimeKind.Utc).AddTicks(9358));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2025, 8, 2, 0, 2, 52, 520, DateTimeKind.Utc).AddTicks(4730),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 488, DateTimeKind.Utc).AddTicks(9847));
        }
    }
}
