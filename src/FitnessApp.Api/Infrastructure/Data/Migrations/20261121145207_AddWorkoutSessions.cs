using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FitnessApp.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkoutSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlanId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PausedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PausedSeconds = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessions", x => x.Id);
                    table.CheckConstraint("CK_WorkoutSessions_PausedSeconds", "\"PausedSeconds\" >= 0");
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_TrainingPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "TrainingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SessionExercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkoutSessionId = table.Column<int>(type: "integer", nullable: false),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetSets = table.Column<int>(type: "integer", nullable: true),
                    TargetRepsMin = table.Column<int>(type: "integer", nullable: true),
                    TargetRepsMax = table.Column<int>(type: "integer", nullable: true),
                    TargetWeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SessionExercises_WorkoutSessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SetEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionExerciseId = table.Column<int>(type: "integer", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    Rpe = table.Column<int>(type: "integer", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    DistanceMeters = table.Column<int>(type: "integer", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetEntries", x => x.Id);
                    table.CheckConstraint("CK_SetEntries_Reps", "\"Reps\" BETWEEN 0 AND 200");
                    table.CheckConstraint("CK_SetEntries_Rpe", "\"Rpe\" IS NULL OR \"Rpe\" BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_SetEntries_WeightKg", "\"WeightKg\" IS NULL OR \"WeightKg\" BETWEEN 0 AND 1000");
                    table.ForeignKey(
                        name: "FK_SetEntries_SessionExercises_SessionExerciseId",
                        column: x => x.SessionExerciseId,
                        principalTable: "SessionExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionExercises_ExerciseId",
                table: "SessionExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionExercises_WorkoutSessionId_Order",
                table: "SessionExercises",
                columns: new[] { "WorkoutSessionId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_SetEntries_SessionExerciseId_SetNumber",
                table: "SetEntries",
                columns: new[] { "SessionExerciseId", "SetNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_PlanId",
                table: "WorkoutSessions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId_StartedAtUtc",
                table: "WorkoutSessions",
                columns: new[] { "UserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId_Status",
                table: "WorkoutSessions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SetEntries");

            migrationBuilder.DropTable(
                name: "SessionExercises");

            migrationBuilder.DropTable(
                name: "WorkoutSessions");
        }
    }
}
