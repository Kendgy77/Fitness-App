using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessApp.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepRangesToPlanExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetReps",
                table: "PlanExercises",
                newName: "TargetRepsMin");

            migrationBuilder.AddColumn<int>(
                name: "TargetRepsMax",
                table: "PlanExercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Hand-written: a plan saved before this change had one reps number, so it becomes a range with both
            // ends equal ("10" -> 10–10). Without this every existing row would get 0 and break the check below.
            migrationBuilder.Sql("UPDATE \"PlanExercises\" SET \"TargetRepsMax\" = \"TargetRepsMin\";");

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 101,
                column: "TargetRepsMax",
                value: 12);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 102,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 12, 8 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 103,
                column: "TargetRepsMax",
                value: 12);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 104,
                column: "TargetRepsMax",
                value: 12);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 105,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 12, 10 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 106,
                column: "TargetRepsMax",
                value: 10);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 201,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 8, 6 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 202,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 8, 6 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 203,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 8, 6 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 204,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 10, 8 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 205,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 12, 8 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 206,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 12, 10 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 301,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 6, 5 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 302,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 8, 6 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 303,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 10, 8 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 304,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 12, 10 });

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 305,
                column: "TargetRepsMax",
                value: 15);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 306,
                columns: new[] { "TargetRepsMax", "TargetRepsMin" },
                values: new object[] { 12, 8 });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlanExercises_RepRange",
                table: "PlanExercises",
                sql: "\"TargetRepsMin\" >= 1 AND \"TargetRepsMax\" >= \"TargetRepsMin\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PlanExercises_RepRange",
                table: "PlanExercises");

            migrationBuilder.DropColumn(
                name: "TargetRepsMax",
                table: "PlanExercises");

            migrationBuilder.RenameColumn(
                name: "TargetRepsMin",
                table: "PlanExercises",
                newName: "TargetReps");

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 102,
                column: "TargetReps",
                value: 10);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 105,
                column: "TargetReps",
                value: 12);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 201,
                column: "TargetReps",
                value: 8);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 202,
                column: "TargetReps",
                value: 8);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 203,
                column: "TargetReps",
                value: 8);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 204,
                column: "TargetReps",
                value: 10);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 205,
                column: "TargetReps",
                value: 10);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 206,
                column: "TargetReps",
                value: 12);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 301,
                column: "TargetReps",
                value: 6);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 302,
                column: "TargetReps",
                value: 8);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 303,
                column: "TargetReps",
                value: 10);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 304,
                column: "TargetReps",
                value: 12);

            migrationBuilder.UpdateData(
                table: "PlanExercises",
                keyColumn: "Id",
                keyValue: 306,
                column: "TargetReps",
                value: 10);
        }
    }
}
