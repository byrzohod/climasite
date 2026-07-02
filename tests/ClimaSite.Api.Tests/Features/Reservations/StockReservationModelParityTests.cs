using System.Data;
using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Api.Tests.Features.Reservations;

/// <summary>
/// INV-01 A2: integration tests build the schema via <c>EnsureCreatedAsync</c> (from the MODEL, not the
/// migration), so the model MUST produce the reservations table, the reserved_quantity column, and the
/// filtered unique index — otherwise every reservation break-probe would run against a phantom schema. This
/// guards that parity.
/// </summary>
public class StockReservationModelParityTests : IntegrationTestBase
{
    public StockReservationModelParityTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Model_Builds_StockReservationsTable_ReservedQuantityColumn_AndFilteredUniqueIndex()
    {
        (await ScalarBoolAsync(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'stock_reservations')"))
            .Should().BeTrue("the stock_reservations table must be built from the model");

        (await ScalarBoolAsync(
            """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'product_variants'
                  AND column_name = 'reserved_quantity'
                  AND is_nullable = 'NO')
            """))
            .Should().BeTrue("reserved_quantity must exist and be NOT NULL");

        // Default text is stored provider-normalised (e.g. "0"); assert it resolves to zero rather than pinning
        // the exact literal.
        var columnDefault = await ScalarStringAsync(
            """
            SELECT column_default FROM information_schema.columns
            WHERE table_name = 'product_variants' AND column_name = 'reserved_quantity'
            """);
        columnDefault.Should().NotBeNullOrEmpty();
        columnDefault.Should().StartWith("0", "reserved_quantity defaults to 0");

        // Postgres normalises the partial-index predicate to "((status)::text = 'Active'::text)", so match the
        // component parts rather than the raw model literal.
        (await ScalarBoolAsync(
            """
            SELECT EXISTS (
                SELECT 1 FROM pg_indexes
                WHERE tablename = 'stock_reservations'
                  AND indexdef ILIKE '%UNIQUE%'
                  AND indexdef ILIKE '%cart_id%'
                  AND indexdef ILIKE '%variant_id%'
                  AND indexdef ILIKE '%WHERE%'
                  AND indexdef ILIKE '%status%'
                  AND indexdef ILIKE '%Active%')
            """))
            .Should().BeTrue("the filtered UNIQUE (cart_id, variant_id) WHERE status='Active' index must exist");
    }

    private async Task<bool> ScalarBoolAsync(string sql)
    {
        var result = await ScalarAsync(sql);
        return result is bool value && value;
    }

    private async Task<string?> ScalarStringAsync(string sql) => (await ScalarAsync(sql))?.ToString();

    private async Task<object?> ScalarAsync(string sql)
    {
        var connection = DbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }
}
