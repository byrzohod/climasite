using ClimaSite.Application.Features.Admin.Installation.Commands;
using ClimaSite.Application.Features.Admin.Installation.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/installation-requests")]
[Authorize(Roles = "Admin")]
public class AdminInstallationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminInstallationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lists installation requests (newest first), with optional status filtering and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRequests([FromQuery] GetAdminInstallationRequestsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Updates an installation request's status (confirm, schedule, mark in progress, complete, cancel).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateInstallationRequestStatusRequest request)
    {
        var command = new UpdateInstallationRequestStatusCommand
        {
            Id = id,
            Status = request.Status,
            ScheduledDate = request.ScheduledDate,
            FinalPrice = request.FinalPrice
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true });
    }
}

public record UpdateInstallationRequestStatusRequest
{
    public string Status { get; init; } = string.Empty;
    public DateTime? ScheduledDate { get; init; }
    public decimal? FinalPrice { get; init; }
}
