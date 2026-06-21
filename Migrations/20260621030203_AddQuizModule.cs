using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyGo.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnswersJson",
                table: "QuizAttempts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "QuizAttempts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Activities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "Activities",
                type: "int",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenDate",
                table: "Activities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeLimitMinutes",
                table: "Activities",
                type: "int",
                nullable: true,
                defaultValue: 30);

            migrationBuilder.CreateTable(
                name: "QuizQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Unica"),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Activities_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizOptions_QuizQuestions_QuizQuestionId",
                        column: x => x.QuizQuestionId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizOptions_QuizQuestionId",
                table: "QuizOptions",
                column: "QuizQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_QuizId_Order",
                table: "QuizQuestions",
                columns: new[] { "QuizId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizOptions");

            migrationBuilder.DropTable(
                name: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "AnswersJson",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "OpenDate",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "TimeLimitMinutes",
                table: "Activities");
        }
    }
}
