using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimaSite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankTransferReservationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_order_id_variant_id",
                table: "stock_reservations",
                columns: new[] { "order_id", "variant_id" },
                unique: true,
                filter: "status = 'Active' AND kind = 'BankTransfer'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_reservations_order_id_variant_id",
                table: "stock_reservations");
        }
    }
}
