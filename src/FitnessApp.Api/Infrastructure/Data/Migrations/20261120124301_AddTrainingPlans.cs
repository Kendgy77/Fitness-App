using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FitnessApp.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingPlans_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanExercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingPlanId = table.Column<int>(type: "integer", nullable: false),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    TargetSets = table.Column<int>(type: "integer", nullable: false),
                    TargetReps = table.Column<int>(type: "integer", nullable: false),
                    TargetWeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlanExercises_TrainingPlans_TrainingPlanId",
                        column: x => x.TrainingPlanId,
                        principalTable: "TrainingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TrainingPlans",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "Name", "Status", "UpdatedAtUtc", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Three times a week, with a rest day in between. Every main muscle group in one session, with simple exercises to learn the technique. Add weight when all sets feel easy.", "Full Body – Beginner", "Active", new DateTimeOffset(new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { 2, new DateTimeOffset(new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Chest, back, shoulders and arms. Pair it with \"Lower Body – Intermediate\" for a four-day upper/lower split. Rest 2–3 minutes between heavy sets.", "Upper Body – Intermediate", "Active", new DateTimeOffset(new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { 3, new DateTimeOffset(new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Legs, glutes and core. Pair it with \"Upper Body – Intermediate\". Warm up with a few light sets of squats before the working sets.", "Lower Body – Intermediate", "Active", new DateTimeOffset(new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                table: "PlanExercises",
                columns: new[] { "Id", "ExerciseId", "Notes", "Order", "TargetReps", "TargetSets", "TargetWeightKg", "TrainingPlanId" },
                values: new object[,]
                {
                    { 101, 38, "", 1, 10, 3, null, 1 },
                    { 102, 7, "", 2, 10, 3, null, 1 },
                    { 103, 14, "", 3, 10, 3, null, 1 },
                    { 104, 21, "", 4, 10, 3, null, 1 },
                    { 105, 43, "", 5, 12, 3, null, 1 },
                    { 106, 56, "", 6, 10, 3, null, 1 },
                    { 201, 1, "", 1, 8, 4, null, 2 },
                    { 202, 13, "", 2, 8, 4, null, 2 },
                    { 203, 20, "", 3, 8, 3, null, 2 },
                    { 204, 15, "", 4, 10, 3, null, 2 },
                    { 205, 27, "", 5, 10, 3, null, 2 },
                    { 206, 32, "", 6, 12, 3, null, 2 },
                    { 301, 36, "", 1, 6, 4, null, 3 },
                    { 302, 42, "", 2, 8, 3, null, 3 },
                    { 303, 41, "", 3, 10, 3, null, 3 },
                    { 304, 39, "", 4, 12, 3, null, 3 },
                    { 305, 46, "", 5, 12, 4, null, 3 },
                    { 306, 52, "", 6, 10, 3, null, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanExercises_ExerciseId",
                table: "PlanExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanExercises_TrainingPlanId_Order",
                table: "PlanExercises",
                columns: new[] { "TrainingPlanId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlans_UserId_UpdatedAtUtc",
                table: "TrainingPlans",
                columns: new[] { "UserId", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanExercises");

            migrationBuilder.DropTable(
                name: "TrainingPlans");
        }
    }
}
