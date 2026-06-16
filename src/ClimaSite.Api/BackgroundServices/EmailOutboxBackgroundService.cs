using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Outbox;

namespace ClimaSite.Api.BackgroundServices;

/// <summary>
/// Hosted worker that periodically drains the email outbox. The actual delivery logic lives in
/// <see cref="IOutboxProcessor"/> (resolved per-tick from a fresh DI scope); this class is only the
/// polling shell. Disabled via <c>Outbox:Enabled = false</c> (e.g. integration tests drive the
/// processor directly).
/// </summary>
public class EmailOutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailOutboxOptions _options;
    private readonly ILogger<EmailOutboxBackgroundService> _logger;

    public EmailOutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        EmailOutboxOptions options,
        ILogger<EmailOutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Email outbox worker is disabled (Outbox:Enabled=false); not polling.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));
        _logger.LogInformation("Email outbox worker started; polling every {Interval}.", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                var sent = await processor.ProcessPendingAsync(stoppingToken);
                if (sent > 0)
                {
                    _logger.LogInformation("Email outbox worker delivered {Count} message(s).", sent);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email outbox drain failed; will retry next interval.");
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

        _logger.LogInformation("Email outbox worker stopping.");
    }
}
