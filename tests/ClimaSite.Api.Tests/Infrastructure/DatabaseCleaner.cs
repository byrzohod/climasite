using ClimaSite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Api.Tests.Infrastructure;

public class DatabaseCleaner
{
    private readonly ApplicationDbContext _context;

    public DatabaseCleaner(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cleans all data from tables while preserving schema.
    /// Uses TRUNCATE with CASCADE for efficiency.
    /// </summary>
    public async Task CleanAsync()
    {
        // Tables to clean in order (respecting foreign keys)
        var tables = new[]
        {
            "order_items",
            "orders",
            "cart_items",
            "carts",
            "product_images",
            "product_specifications",
            "products",
            "categories",
            "user_addresses",
            "users",
            "audit_logs"
        };

        foreach (var table in tables)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    $"TRUNCATE TABLE {table} CASCADE");
            }
            catch (Exception)
            {
                // Table might not exist yet, ignore
            }
        }

        // Reset sequences
        try
        {
            await _context.Database.ExecuteSqlRawAsync(@"
                DO $$
                DECLARE
                    seq RECORD;
                BEGIN
                    FOR seq IN SELECT sequencename FROM pg_sequences WHERE schemaname = 'public'
                    LOOP
                        EXECUTE 'ALTER SEQUENCE ' || seq.sequencename || ' RESTART WITH 1';
                    END LOOP;
                END $$;");
        }
        catch (Exception)
        {
            // Ignore sequence reset errors
        }
    }

    /// <summary>
    /// Cleans specific data created by a test using correlation ID.
    /// </summary>
    public async Task CleanByCorrelationIdAsync(Guid correlationId)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM orders WHERE correlation_id = {0}", correlationId);
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM users WHERE correlation_id = {0}", correlationId);
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM products WHERE correlation_id = {0}", correlationId);
        }
        catch (Exception)
        {
            // Ignore cleanup errors - tables might not have correlation_id
        }
    }
}
