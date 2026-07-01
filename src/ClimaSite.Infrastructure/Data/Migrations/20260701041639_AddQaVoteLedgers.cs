using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimaSite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQaVoteLedgers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_answer_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_helpful = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_answer_votes", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_answer_votes_product_answers_answer_id",
                        column: x => x.answer_id,
                        principalTable: "product_answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_answer_votes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_question_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_question_votes", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_question_votes_product_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "product_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_question_votes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_answer_votes_answer_id",
                table: "product_answer_votes",
                column: "answer_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_answer_votes_answer_id_user_id",
                table: "product_answer_votes",
                columns: new[] { "answer_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_answer_votes_user_id",
                table: "product_answer_votes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_question_votes_question_id",
                table: "product_question_votes",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_question_votes_question_id_user_id",
                table: "product_question_votes",
                columns: new[] { "question_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_question_votes_user_id",
                table: "product_question_votes",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_answer_votes");

            migrationBuilder.DropTable(
                name: "product_question_votes");
        }
    }
}
