using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Installation.DTOs;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Installation.Commands;

public record CreateInstallationRequestCommand : IRequest<InstallationRequestDto>
{
    public Guid ProductId { get; init; }
    public Guid? UserId { get; init; }
    public Guid? OrderId { get; init; }
    public string InstallationType { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public DateTime? PreferredDate { get; init; }
    public string? PreferredTimeSlot { get; init; }
    public string? Notes { get; init; }
}

public class CreateInstallationRequestCommandValidator : AbstractValidator<CreateInstallationRequestCommand>
{
    public CreateInstallationRequestCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.InstallationType)
            .NotEmpty().WithMessage("Installation type is required.")
            .Must(BeValidInstallationType).WithMessage("Invalid installation type.");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.CustomerPhone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(255).WithMessage("Address cannot exceed 255 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters.");

        RuleFor(x => x.PreferredDate)
            .Must(date => !date.HasValue || date.Value > DateTime.UtcNow)
            .WithMessage("Preferred date must be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters.");
    }

    private static bool BeValidInstallationType(string type)
    {
        return Enum.TryParse<InstallationType>(type, true, out _);
    }
}

public class CreateInstallationRequestCommandHandler : IRequestHandler<CreateInstallationRequestCommand, InstallationRequestDto>
{
    private readonly IApplicationDbContext _context;

    public CreateInstallationRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InstallationRequestDto> Handle(
        CreateInstallationRequestCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {request.ProductId} not found.");
        }

        var installationType = Enum.Parse<InstallationType>(request.InstallationType, true);
        var estimatedPrice = CalculateEstimatedPrice(installationType, product.BasePrice);

        var installationRequest = new InstallationRequest(
            request.ProductId,
            product.Name,
            installationType,
            request.CustomerName,
            request.CustomerEmail,
            request.CustomerPhone,
            request.AddressLine1,
            request.City,
            request.PostalCode,
            request.Country,
            estimatedPrice
        );

        if (request.UserId.HasValue)
        {
            installationRequest.SetUser(request.UserId.Value);
        }

        if (request.OrderId.HasValue)
        {
            installationRequest.SetOrder(request.OrderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.AddressLine2))
        {
            installationRequest.SetAddressLine2(request.AddressLine2);
        }

        if (request.PreferredDate.HasValue || !string.IsNullOrWhiteSpace(request.PreferredTimeSlot))
        {
            installationRequest.SetPreferredSchedule(request.PreferredDate, request.PreferredTimeSlot);
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            installationRequest.SetNotes(request.Notes);
        }

        _context.InstallationRequests.Add(installationRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return new InstallationRequestDto
        {
            Id = installationRequest.Id,
            ProductId = installationRequest.ProductId,
            ProductName = installationRequest.ProductName,
            InstallationType = installationRequest.InstallationType.ToString(),
            Status = installationRequest.Status.ToString(),
            CustomerName = installationRequest.CustomerName,
            CustomerEmail = installationRequest.CustomerEmail,
            CustomerPhone = installationRequest.CustomerPhone,
            AddressLine1 = installationRequest.AddressLine1,
            AddressLine2 = installationRequest.AddressLine2,
            City = installationRequest.City,
            PostalCode = installationRequest.PostalCode,
            Country = installationRequest.Country,
            PreferredDate = installationRequest.PreferredDate,
            PreferredTimeSlot = installationRequest.PreferredTimeSlot,
            ScheduledDate = installationRequest.ScheduledDate,
            Notes = installationRequest.Notes,
            EstimatedPrice = installationRequest.EstimatedPrice,
            FinalPrice = installationRequest.FinalPrice,
            CreatedAt = installationRequest.CreatedAt
        };
    }

    private static decimal CalculateEstimatedPrice(InstallationType type, decimal productPrice)
    {
        return type switch
        {
            InstallationType.Standard => Math.Round(productPrice * 0.15m, 2), // 15% of product price
            InstallationType.Premium => Math.Round(productPrice * 0.25m, 2),  // 25% of product price
            InstallationType.Express => Math.Round(productPrice * 0.35m, 2),  // 35% of product price (includes priority scheduling)
            _ => 0
        };
    }
}
