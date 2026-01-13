using ClimaSite.Application.Features.Installation.Commands;
using ClimaSite.Application.Features.Installation.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstallationController : ControllerBase
{
    private readonly IMediator _mediator;

    public InstallationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get installation options for a product
    /// </summary>
    [HttpGet("options/{productId:guid}")]
    public async Task<IActionResult> GetInstallationOptions(Guid productId)
    {
        var query = new GetInstallationOptionsQuery { ProductId = productId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new installation request
    /// </summary>
    [HttpPost("requests")]
    public async Task<IActionResult> CreateInstallationRequest([FromBody] CreateInstallationRequestRequest request)
    {
        var command = new CreateInstallationRequestCommand
        {
            ProductId = request.ProductId,
            InstallationType = request.InstallationType,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            CustomerPhone = request.CustomerPhone,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            PreferredDate = request.PreferredDate,
            PreferredTimeSlot = request.PreferredTimeSlot,
            Notes = request.Notes
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetInstallationOptions), new { productId = request.ProductId }, result);
    }
}

public record CreateInstallationRequestRequest
{
    public Guid ProductId { get; init; }
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
