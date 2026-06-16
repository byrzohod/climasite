using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Features.Contact.Commands;

public record CreateContactMessageCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public class CreateContactMessageCommandValidator : AbstractValidator<CreateContactMessageCommand>
{
    public CreateContactMessageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(5000);
    }
}

public class CreateContactMessageCommandHandler : IRequestHandler<CreateContactMessageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailOutbox _emailOutbox;
    private readonly ContactOptions _options;
    private readonly ILogger<CreateContactMessageCommandHandler> _logger;

    public CreateContactMessageCommandHandler(
        IApplicationDbContext context,
        IEmailOutbox emailOutbox,
        ContactOptions options,
        ILogger<CreateContactMessageCommandHandler> logger)
    {
        _context = context;
        _emailOutbox = emailOutbox;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateContactMessageCommand request, CancellationToken cancellationToken)
    {
        var contactMessage = new ContactMessage(request.Name, request.Email, request.Subject, request.Message);
        _context.ContactMessages.Add(contactMessage);

        // GAP-05: notify the business in the SAME transaction as the persisted enquiry, so the row
        // and its notification email commit together (no lead silently lost). The reply-to address
        // is included in the body since the outbox sends from the system address.
        var subject = $"[Contact] {contactMessage.Subject}";
        var body =
            $"New contact enquiry from {contactMessage.Name} <{contactMessage.Email}>\n\n" +
            $"{contactMessage.Message}";
        _emailOutbox.Add(OutboxMessage.ForGeneric(_options.RecipientEmail, subject, body));

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Contact message {MessageId} stored and queued for {Recipient}.",
            contactMessage.Id, _options.RecipientEmail);

        return Result<Guid>.Success(contactMessage.Id);
    }
}
