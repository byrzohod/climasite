using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Installation.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Installation.Queries;

public record GetInstallationOptionsQuery : IRequest<ProductInstallationOptionsDto>
{
    public Guid ProductId { get; init; }
}

public class GetInstallationOptionsQueryHandler : IRequestHandler<GetInstallationOptionsQuery, ProductInstallationOptionsDto>
{
    private readonly IApplicationDbContext _context;

    public GetInstallationOptionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductInstallationOptionsDto> Handle(
        GetInstallationOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {request.ProductId} not found.");
        }

        // Check if installation is available based on product category or attributes
        var installationAvailable = IsInstallationAvailable(product);

        var options = new List<InstallationOptionDto>();

        if (installationAvailable)
        {
            options.Add(new InstallationOptionDto
            {
                Type = InstallationType.Standard.ToString(),
                Name = "Standard Installation",
                Description = "Professional installation by certified technicians",
                Price = Math.Round(product.BasePrice * 0.15m, 2),
                Features = new[]
                {
                    "Certified technician",
                    "Equipment setup",
                    "Basic testing",
                    "Standard scheduling (5-7 days)"
                },
                EstimatedDays = 7
            });

            options.Add(new InstallationOptionDto
            {
                Type = InstallationType.Premium.ToString(),
                Name = "Premium Installation",
                Description = "Full-service installation with extended warranty coverage",
                Price = Math.Round(product.BasePrice * 0.25m, 2),
                Features = new[]
                {
                    "Senior certified technician",
                    "Complete system integration",
                    "Performance optimization",
                    "Extended testing & calibration",
                    "1-year installation warranty",
                    "Priority scheduling (3-5 days)"
                },
                EstimatedDays = 5
            });

            options.Add(new InstallationOptionDto
            {
                Type = InstallationType.Express.ToString(),
                Name = "Express Installation",
                Description = "Fast-track installation with priority scheduling",
                Price = Math.Round(product.BasePrice * 0.35m, 2),
                Features = new[]
                {
                    "Senior certified technician",
                    "Complete system integration",
                    "Performance optimization",
                    "Extended testing & calibration",
                    "2-year installation warranty",
                    "Express scheduling (1-2 days)",
                    "Weekend availability"
                },
                EstimatedDays = 2
            });
        }

        return new ProductInstallationOptionsDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            InstallationAvailable = installationAvailable,
            Options = options
        };
    }

    private static bool IsInstallationAvailable(Product product)
    {
        // Installation is available for products over a certain price threshold
        // and that are not accessories or small items
        return product.BasePrice >= 200;
    }
}
