using ClimaSite.Application.Common.Interfaces;

namespace ClimaSite.Api.Services;

/// <summary>
/// Scoped holder for the verified guest-session id (INV-01 Wave A0). The guest-session middleware — which
/// depends on this concrete type — sets <see cref="GuestSessionId"/> once per request; controllers depend on
/// the read-only <see cref="IGuestSessionAccessor"/>. Both are registered against the SAME scoped instance so
/// what the middleware writes is what the controller reads.
/// </summary>
public sealed class GuestSessionAccessor : IGuestSessionAccessor
{
    public string? GuestSessionId { get; set; }
}
