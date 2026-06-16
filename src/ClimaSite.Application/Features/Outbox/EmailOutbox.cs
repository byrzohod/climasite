using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;

namespace ClimaSite.Application.Features.Outbox;

/// <inheritdoc />
public class EmailOutbox : IEmailOutbox
{
    private readonly IApplicationDbContext _context;

    public EmailOutbox(IApplicationDbContext context)
    {
        _context = context;
    }

    public void Add(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _context.OutboxMessages.Add(message);
    }

    public async Task QueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
