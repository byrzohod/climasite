using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimaSite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "review_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_helpful = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_votes", x => x.id);
                    table.ForeignKey(
                        name: "FK_review_votes_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_review_votes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_review_votes_review_id",
                table: "review_votes",
                column: "review_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_votes_review_id_user_id",
                table: "review_votes",
                columns: new[] { "review_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_votes_user_id",
                table: "review_votes",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "review_votes");
        }
    }
}
