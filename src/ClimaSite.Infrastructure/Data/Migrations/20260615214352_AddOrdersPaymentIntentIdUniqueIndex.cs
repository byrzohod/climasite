using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimaSite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersPaymentIntentIdUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_payment_intent_id",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "IX_orders_payment_intent_id",
                table: "orders",
                column: "payment_intent_id",
                unique: true,
                filter: "payment_intent_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_payment_intent_id",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "IX_orders_payment_intent_id",
                table: "orders",
                column: "payment_intent_id");
        }
    }
}
