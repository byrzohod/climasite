using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimaSite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductQuestionsAndAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    banner_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    website_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    country_of_origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    founded_year = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    meta_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "category_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    meta_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_translations", x => x.id);
                    table.ForeignKey(
                        name: "FK_category_translations_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    question_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    asker_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    asker_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    helpful_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_questions_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_questions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    minimum_order_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    banner_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    thumbnail_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "brand_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    meta_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brand_translations", x => x.id);
                    table.ForeignKey(
                        name: "FK_brand_translations_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    answer_text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    answerer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_official = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    helpful_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    unhelpful_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_answers_product_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "product_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_answers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "promotion_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_promotion_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_promotion_products_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_translations", x => x.id);
                    table.ForeignKey(
                        name: "FK_promotion_translations_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_brand_translations_brand_id_language_code",
                table: "brand_translations",
                columns: new[] { "brand_id", "language_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_brands_is_active",
                table: "brands",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_brands_is_featured",
                table: "brands",
                column: "is_featured");

            migrationBuilder.CreateIndex(
                name: "IX_brands_name",
                table: "brands",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_brands_slug",
                table: "brands",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_brands_sort_order",
                table: "brands",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "IX_category_translations_category_id_language_code",
                table: "category_translations",
                columns: new[] { "category_id", "language_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_answers_created_at",
                table: "product_answers",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_product_answers_is_official",
                table: "product_answers",
                column: "is_official");

            migrationBuilder.CreateIndex(
                name: "IX_product_answers_question_id",
                table: "product_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_answers_status",
                table: "product_answers",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_product_answers_user_id",
                table: "product_answers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_questions_created_at",
                table: "product_questions",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_product_questions_product_id",
                table: "product_questions",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_questions_status",
                table: "product_questions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_product_questions_user_id",
                table: "product_questions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_products_product_id",
                table: "promotion_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_products_promotion_id_product_id",
                table: "promotion_products",
                columns: new[] { "promotion_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_translations_promotion_id_language_code",
                table: "promotion_translations",
                columns: new[] { "promotion_id", "language_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotions_code",
                table: "promotions",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_is_active",
                table: "promotions",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_is_featured",
                table: "promotions",
                column: "is_featured");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_slug",
                table: "promotions",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotions_start_date_end_date",
                table: "promotions",
                columns: new[] { "start_date", "end_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brand_translations");

            migrationBuilder.DropTable(
                name: "category_translations");

            migrationBuilder.DropTable(
                name: "product_answers");

            migrationBuilder.DropTable(
                name: "promotion_products");

            migrationBuilder.DropTable(
                name: "promotion_translations");

            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropTable(
                name: "product_questions");

            migrationBuilder.DropTable(
                name: "promotions");
        }
    }
}
