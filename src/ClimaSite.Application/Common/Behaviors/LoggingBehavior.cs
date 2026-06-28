using System.Diagnostics;
using ClimaSite.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId?.ToString() ?? "Anonymous";

        // SECURITY: log a redacted projection — never the raw command. The raw command can carry
        // credentials (LoginCommand/RegisterCommand.Password, GoogleSignInCommand.IdToken); destructuring
        // it with {@Request} would write those to logs in cleartext (OWASP A09 / GDPR). See LogSanitizer.
        // Computed lazily + only when the consuming level is enabled, so the projection cost (reflection +
        // bounded collection materialisation) is skipped when request logging is off.
        IReadOnlyDictionary<string, object?>? safeRequest = null;
        if (_logger.IsEnabled(LogLevel.Information))
        {
            safeRequest = LogSanitizer.Redact(request);
            _logger.LogInformation(
                "ClimaSite Request: {Name} {@UserId} {@Request}",
                requestName, userId, safeRequest);
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500 && _logger.IsEnabled(LogLevel.Warning))
        {
            safeRequest ??= LogSanitizer.Redact(request);
            _logger.LogWarning(
                "ClimaSite Long Running Request: {Name} ({ElapsedMilliseconds} ms) {@UserId} {@Request}",
                requestName, elapsedMilliseconds, userId, safeRequest);
        }

        return response;
    }
}
