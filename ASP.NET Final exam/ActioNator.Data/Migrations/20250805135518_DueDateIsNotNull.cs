using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ActioNator.Data.Migrations
{
    /// <inheritdoc />
    public partial class DueDateIsNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                defaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 144, DateTimeKind.Utc).AddTicks(1135),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 502, DateTimeKind.Utc).AddTicks(2952));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 142, DateTimeKind.Utc).AddTicks(8638),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 501, DateTimeKind.Utc).AddTicks(4003));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 138, DateTimeKind.Utc).AddTicks(414),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 497, DateTimeKind.Utc).AddTicks(207));

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 136, DateTimeKind.Utc).AddTicks(9074),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 495, DateTimeKind.Utc).AddTicks(7320));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 135, DateTimeKind.Utc).AddTicks(5991),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 494, DateTimeKind.Utc).AddTicks(4857));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 134, DateTimeKind.Utc).AddTicks(6331),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 493, DateTimeKind.Utc).AddTicks(5458));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 130, DateTimeKind.Utc).AddTicks(1929),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 488, DateTimeKind.Utc).AddTicks(9358));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 130, DateTimeKind.Utc).AddTicks(2358),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 488, DateTimeKind.Utc).AddTicks(9847));

            migrationBuilder.InsertData(
                table: "ExerciseTemplates",
                columns: new[] { "Id", "ImageUrl", "Name", "TargetedMuscle" },
                values: new object[,]
                {
                    { new Guid("1cab9aa9-b32b-405f-907f-135d83d90321"), "~/images/exercise/jumpingJacksImage.png", "Jumping Jacks", "Cardio" },
                    { new Guid("1fae1c4f-ef0b-470c-b5be-437e0621160d"), "~/images/exercise/squatImage.webp", "Squat", "Legs" },
                    { new Guid("2360c19f-7ad2-4d19-9d34-4a3b05550902"), "~/images/exercise/mountainClimbersImage.png", "Mountain Climbers", "Full Body" },
                    { new Guid("292e9c65-edfb-4c13-8fe3-7c61db68eb84"), "~/images/exercise/bentOverRowImage.webp", "Bent Over Row", "Upper Back" },
                    { new Guid("35933fa3-23c1-47e8-ab8e-e12af4b0652a"), "~/images/exercise/legRaisesImage.webp", "Leg Raises", "Lower Abs" },
                    { new Guid("4e70411f-581c-4640-8ac2-fe6242db81df"), "~/images/exercise/calfRaiseImage.jpeg", "Calf Raise", "Calves" },
                    { new Guid("5a40751d-49dd-46ea-9105-1a0d9bc9bc3e"), "~/images/exercise/wallSitImage.png", "Wall Sit", "Quadriceps" },
                    { new Guid("5d2ef3e1-ac04-455b-9148-a5ef0261b2e3"), "~/images/exercise/pushUpImage.png", "Push Up", "Chest" },
                    { new Guid("6b48b2e4-d6d7-434c-a170-4300119c6b13"), "~/images/exercise/tricepDipImage.png", "Tricep Dip", "Triceps" },
                    { new Guid("6fcb257f-83a6-4417-aeb5-7a475fb9878b"), "~/images/exercise/lungesImage.webp", "Lunges", "Legs" },
                    { new Guid("7bea8725-ee55-4cfe-a4be-8bd62352bb93"), "~/images/exercise/highKneesImage.webp", "High Knees", "Cardio" },
                    { new Guid("7ca7fa16-67db-4061-b714-753c0fb0b495"), "~/images/exercise/bicepCurlImage.webp", "Bicep Curl", "Biceps" },
                    { new Guid("8a5a3625-7488-4dda-83be-c09b37efad72"), "~/images/exercise/gluteBridgeImage.webp", "Glute Bridge", "Glutes" },
                    { new Guid("8e19e3c4-9059-48ec-b2fe-81e7301a5b39"), "~/images/exercise/pullUpImage.png", "Pull Up", "Back" },
                    { new Guid("8e511799-98cc-42cb-874e-77992e7af199"), "~/images/exercise/plankImage.png", "Plank", "Core" },
                    { new Guid("9c1bdf83-a5e3-4bf6-bbdf-dae04bdec374"), "~/images/exercise/chestFlyImage.webp", "Chest Fly", "Chest" },
                    { new Guid("9d8a7d65-283d-45a8-9541-b407d7e57e4b"), "~/images/exercise/deadliftImage.png", "Deadlift", "Back" },
                    { new Guid("ab8e2f1c-a0e7-47b8-9f5f-6c2ba2072ce0"), "~/images/exercise/sidePlankImage.png", "Side Plank", "Obliques" },
                    { new Guid("bf65b8c5-4ed3-49c2-83ab-b37806612215"), "~/images/exercise/burpeesImage.webp", "Burpees", "Full Body" },
                    { new Guid("eb49ef05-eb1d-4dcd-80f9-808586d31319"), "~/images/exercise/russianTwistsImage.webp", "Russian Twists", "Obliques" },
                    { new Guid("f46b70e9-a97c-48d0-bcf3-e82ff81668c8"), "~/images/exercise/shoulderPressImage.webp", "Shoulder Press", "Shoulders" },
                    { new Guid("f69f5dfe-7033-4188-bcbe-bb6fdf508c1f"), "~/images/exercise/toeTouchesImage.webp", "Toe Touches", "Hamstrings" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("1cab9aa9-b32b-405f-907f-135d83d90321"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("1fae1c4f-ef0b-470c-b5be-437e0621160d"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("2360c19f-7ad2-4d19-9d34-4a3b05550902"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("292e9c65-edfb-4c13-8fe3-7c61db68eb84"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("35933fa3-23c1-47e8-ab8e-e12af4b0652a"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4e70411f-581c-4640-8ac2-fe6242db81df"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("5a40751d-49dd-46ea-9105-1a0d9bc9bc3e"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("5d2ef3e1-ac04-455b-9148-a5ef0261b2e3"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("6b48b2e4-d6d7-434c-a170-4300119c6b13"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("6fcb257f-83a6-4417-aeb5-7a475fb9878b"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("7bea8725-ee55-4cfe-a4be-8bd62352bb93"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("7ca7fa16-67db-4061-b714-753c0fb0b495"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("8a5a3625-7488-4dda-83be-c09b37efad72"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("8e19e3c4-9059-48ec-b2fe-81e7301a5b39"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("8e511799-98cc-42cb-874e-77992e7af199"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("9c1bdf83-a5e3-4bf6-bbdf-dae04bdec374"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("9d8a7d65-283d-45a8-9541-b407d7e57e4b"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("ab8e2f1c-a0e7-47b8-9f5f-6c2ba2072ce0"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("bf65b8c5-4ed3-49c2-83ab-b37806612215"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("eb49ef05-eb1d-4dcd-80f9-808586d31319"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("f46b70e9-a97c-48d0-bcf3-e82ff81668c8"));

            migrationBuilder.DeleteData(
                table: "ExerciseTemplates",
                keyColumn: "Id",
                keyValue: new Guid("f69f5dfe-7033-4188-bcbe-bb6fdf508c1f"));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 502, DateTimeKind.Utc).AddTicks(2952),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 144, DateTimeKind.Utc).AddTicks(1135));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PostedAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 501, DateTimeKind.Utc).AddTicks(4003),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 142, DateTimeKind.Utc).AddTicks(8638));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 497, DateTimeKind.Utc).AddTicks(207),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 138, DateTimeKind.Utc).AddTicks(414));

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Goals",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 495, DateTimeKind.Utc).AddTicks(7320),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 136, DateTimeKind.Utc).AddTicks(9074));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 494, DateTimeKind.Utc).AddTicks(4857),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 135, DateTimeKind.Utc).AddTicks(5991));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 493, DateTimeKind.Utc).AddTicks(5458),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 134, DateTimeKind.Utc).AddTicks(6331));

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 488, DateTimeKind.Utc).AddTicks(9358),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 130, DateTimeKind.Utc).AddTicks(1929));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2025, 8, 2, 9, 32, 43, 488, DateTimeKind.Utc).AddTicks(9847),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2025, 8, 5, 13, 55, 15, 130, DateTimeKind.Utc).AddTicks(2358));

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
    }
}
