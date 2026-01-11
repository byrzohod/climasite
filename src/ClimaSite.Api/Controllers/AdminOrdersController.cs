using ClimaSite.Application.Features.Admin.Orders.Commands;
using ClimaSite.Application.Features.Admin.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminOrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] GetAdminOrdersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var result = await _mediator.Send(new GetAdminOrderByIdQuery { Id = id });
        if (result == null)
        {
            return NotFound(new { message = "Order not found" });
        }
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        var command = new UpdateOrderStatusCommand
        {
            OrderId = id,
            Status = request.Status,
            Note = request.Note,
            NotifyCustomer = request.NotifyCustomer
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }
        return NoContent();
    }

    [HttpPut("{id:guid}/shipping")]
    public async Task<IActionResult> UpdateShippingInfo(Guid id, [FromBody] UpdateShippingInfoRequest request)
    {
        var command = new UpdateShippingInfoCommand
        {
            OrderId = id,
            TrackingNumber = request.TrackingNumber,
            ShippingMethod = request.ShippingMethod,
            MarkAsShipped = request.MarkAsShipped
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }
        return NoContent();
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddOrderNote(Guid id, [FromBody] AddOrderNoteRequest request)
    {
        var command = new AddOrderNoteCommand
        {
            OrderId = id,
            Note = request.Note
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Note added" });
    }
}

public record UpdateOrderStatusRequest
{
    public string Status { get; init; } = string.Empty;
    public string? Note { get; init; }
    public bool NotifyCustomer { get; init; } = true;
}

public record UpdateShippingInfoRequest
{
    public string? TrackingNumber { get; init; }
    public string? ShippingMethod { get; init; }
    public bool MarkAsShipped { get; init; }
}

public record AddOrderNoteRequest
{
    public string Note { get; init; } = string.Empty;
}
