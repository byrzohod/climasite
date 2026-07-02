using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimaSite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reserved_quantity",
                table: "product_variants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cart_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_intent_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservations", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_reservations_carts_cart_id",
                        column: x => x.cart_id,
                        principalTable: "carts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stock_reservations_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_cart_id_variant_id",
                table: "stock_reservations",
                columns: new[] { "cart_id", "variant_id" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_order_id",
                table: "stock_reservations",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_payment_intent_id",
                table: "stock_reservations",
                column: "payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_status_expires_at",
                table: "stock_reservations",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_variant_id_status",
                table: "stock_reservations",
                columns: new[] { "variant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_reservations");

            migrationBuilder.DropColumn(
                name: "reserved_quantity",
                table: "product_variants");
        }
    }
}
