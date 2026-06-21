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
        await _context.Database.ExecuteSqlRawAsync("""
            DO $$
            DECLARE
                tables_to_clean text;
            BEGIN
                SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
                INTO tables_to_clean
                FROM pg_tables
                WHERE schemaname = 'public'
                  AND tablename NOT IN ('__EFMigrationsHistory', 'roles', 'role_claims');

                IF tables_to_clean IS NOT NULL THEN
                    EXECUTE 'TRUNCATE TABLE ' || tables_to_clean || ' CASCADE';
                END IF;
            END $$;
            """);

        // Reset sequences
        try
        {
            await _context.Database.ExecuteSqlRawAsync("""
                DO $$
                DECLARE
                    sequence_name text;
                BEGIN
                    FOR sequence_name IN
                        SELECT format('%I.%I', schemaname, sequencename)
                        FROM pg_sequences
                        WHERE schemaname = 'public'
                    LOOP
                        EXECUTE 'ALTER SEQUENCE ' || sequence_name || ' RESTART WITH 1';
                    END LOOP;
                END $$;
                """);
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
