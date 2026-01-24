using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Orders.Commands;
using ClimaSite.Application.Features.Orders.DTOs;
using ClimaSite.Application.Features.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new order. Supports both authenticated users and guest checkout.
    /// For guest checkout, provide GuestSessionId in the request body.
    /// The handler validates that either the user is authenticated OR a GuestSessionId is provided.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetOrder), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Get paginated list of orders for the authenticated user.
    /// </summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<OrderBriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "date",
        [FromQuery] string sortDirection = "desc")
    {
        var query = new GetUserOrdersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SearchQuery = search,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("statuses")]
    public IActionResult GetOrderStatuses()
    {
        var statuses = Enum.GetNames<Core.Entities.OrderStatus>();
        return Ok(statuses);
    }

    /// <summary>
    /// Get order details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var query = new GetOrderByIdQuery { OrderId = id };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("by-number/{orderNumber}")]
    public async Task<IActionResult> GetOrderByNumber(string orderNumber)
    {
        var query = new GetOrderByNumberQuery { OrderNumber = orderNumber };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest? request = null)
    {
        var command = new CancelOrderCommand
        {
            OrderId = id,
            CancellationReason = request?.Reason
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("{id:guid}/reorder")]
    public async Task<IActionResult> Reorder(Guid id, [FromHeader(Name = "X-Session-Id")] string? sessionId = null)
    {
        var command = new ReorderCommand
        {
            OrderId = id,
            GuestSessionId = sessionId
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpGet("{id:guid}/invoice")]
    public async Task<IActionResult> DownloadInvoice(Guid id)
    {
        var query = new GenerateInvoiceQuery { OrderId = id };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return File(result.Value!.PdfContent, result.Value.ContentType, result.Value.FileName);
    }
}

public record CancelOrderRequest
{
    public string? Reason { get; init; }
}
