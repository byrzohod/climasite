using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;

namespace ClimaSite.Api.BackgroundServices;

/// <summary>
/// Hosted worker that periodically expires elapsed card holds and reconciles the touched variants' counters
/// (INV-01 A2). It is the SOLE releaser of expired holds — the reservation invariant keeps an unswept expired
/// hold counting until this runs, so the store is briefly pessimistic (never oversells). The sweep logic lives
/// in <see cref="IStockReservationService.SweepExpiredHoldsAsync"/> (resolved per-tick from a fresh DI scope);
/// this class is only the polling shell. Disabled in the Testing integration env (tests drive the sweep
/// directly for determinism) and via <c>Reservations:Sweeper:Enabled=false</c> — which is fail-fast-rejected in
/// Production at startup (see Program.cs), since an unswept store leaks stock forever.
/// </summary>
public class StockReservationSweeperBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReservationOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<StockReservationSweeperBackgroundService> _logger;

    public StockReservationSweeperBackgroundService(
        IServiceScopeFactory scopeFactory,
        ReservationOptions options,
        IHostEnvironment environment,
        ILogger<StockReservationSweeperBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_environment.IsEnvironment("Testing"))
        {
            _logger.LogInformation("Reservation sweeper is disabled in the Testing environment; not polling.");
            return;
        }

        if (!_options.Sweeper.Enabled)
        {
            _logger.LogInformation("Reservation sweeper is disabled (Reservations:Sweeper:Enabled=false); not polling.");
            return;
        }

        var interval = TimeSpan.FromSeconds(_options.Sweeper.EffectivePollIntervalSeconds);
        var batchSize = _options.Sweeper.EffectiveBatchSize;
        _logger.LogInformation("Reservation sweeper started; polling every {Interval}.", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reservations = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
                var expired = await reservations.SweepExpiredHoldsAsync(batchSize, stoppingToken);
                if (expired > 0)
                {
                    _logger.LogInformation("Reservation sweeper expired {Count} hold(s).", expired);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reservation sweep failed; will retry next interval.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Reservation sweeper stopping.");
    }
}
