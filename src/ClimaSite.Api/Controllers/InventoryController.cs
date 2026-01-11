using ClimaSite.Application.Features.Inventory.Commands;
using ClimaSite.Application.Features.Inventory.DTOs;
using ClimaSite.Application.Features.Inventory.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetInventory([FromQuery] GetInventoryListQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("{variantId:guid}/stock")]
    public async Task<IActionResult> AdjustStock(Guid variantId, [FromBody] AdjustStockRequest request)
    {
        var command = new AdjustStockCommand
        {
            VariantId = variantId,
            QuantityChange = request.QuantityChange,
            Reason = request.Reason,
            Notes = request.Notes
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }
        return NoContent();
    }

    [HttpPut("{variantId:guid}/threshold")]
    public async Task<IActionResult> SetLowStockThreshold(Guid variantId, [FromBody] SetThresholdRequest request)
    {
        var command = new SetLowStockThresholdCommand
        {
            VariantId = variantId,
            Threshold = request.Threshold
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }
        return NoContent();
    }

    [HttpPost("bulk-adjust")]
    public async Task<IActionResult> BulkAdjustStock([FromBody] BulkAdjustStockRequest request)
    {
        var command = new BulkAdjustStockCommand
        {
            Adjustments = request.Adjustments.Select(a => new StockAdjustmentItem
            {
                VariantId = a.VariantId,
                NewQuantity = a.NewQuantity
            }).ToList(),
            Reason = request.Reason,
            Notes = request.Notes
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Value);
    }
}

public record AdjustStockRequest
{
    public int QuantityChange { get; init; }
    public StockAdjustmentReason Reason { get; init; }
    public string? Notes { get; init; }
}

public record SetThresholdRequest
{
    public int Threshold { get; init; }
}

public record BulkAdjustStockRequest
{
    public List<BulkAdjustmentItem> Adjustments { get; init; } = [];
    public StockAdjustmentReason Reason { get; init; }
    public string? Notes { get; init; }
}

public record BulkAdjustmentItem
{
    public Guid VariantId { get; init; }
    public int NewQuantity { get; init; }
}
