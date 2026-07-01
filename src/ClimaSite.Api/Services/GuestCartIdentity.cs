using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Cart.Commands;
using MediatR;

namespace ClimaSite.Api.Services;

/// <summary>
/// Resolves the guest id that a cart/checkout command should act on (INV-01 A1). The trusted server-minted
/// signed cookie id (published on <see cref="IGuestSessionAccessor"/> by the guest-session middleware) is
/// authoritative; a client-supplied legacy id is only a fallback when no cookie was established. Shared by the
/// cart, payments and orders controllers so the "migrate-then-resolve" step is defined once.
/// </summary>
public interface IGuestCartIdentity
{
    /// <summary>
    /// If a returning guest presents BOTH a trusted cookie id and a DIFFERENT non-empty legacy id, first folds
    /// the legacy cart onto the cookie id (<see cref="MigrateGuestCartCommand"/>), then returns the id the
    /// command should use: the cookie id when present, else the client-supplied id when
    /// <see cref="GuestSessionOptions.AllowLegacyId"/> is enabled, else <see langword="null"/>.
    /// </summary>
    Task<string?> ResolveAsync(string? clientSuppliedId, CancellationToken cancellationToken = default);
}

public sealed class GuestCartIdentity : IGuestCartIdentity
{
    private readonly IGuestSessionAccessor _guestSession;
    private readonly GuestSessionOptions _options;
    private readonly IMediator _mediator;

    public GuestCartIdentity(
        IGuestSessionAccessor guestSession,
        GuestSessionOptions options,
        IMediator mediator)
    {
        _guestSession = guestSession;
        _options = options;
        _mediator = mediator;
    }

    public async Task<string?> ResolveAsync(string? clientSuppliedId, CancellationToken cancellationToken = default)
    {
        var cookieId = _guestSession.GuestSessionId;

        if (!string.IsNullOrEmpty(cookieId)
            && !string.IsNullOrEmpty(clientSuppliedId)
            && !string.Equals(clientSuppliedId, cookieId, StringComparison.Ordinal))
        {
            // Steady-state no-op (a cheap EXISTS); only a returning legacy guest actually migrates.
            await _mediator.Send(new MigrateGuestCartCommand(clientSuppliedId, cookieId), cancellationToken);
        }

        return cookieId ?? (_options.AllowLegacyId ? clientSuppliedId : null);
    }
}
