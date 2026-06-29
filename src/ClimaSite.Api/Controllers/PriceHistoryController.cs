using ClimaSite.Api.Common;
using ClimaSite.Application.Features.PriceHistory.DTOs;
using ClimaSite.Application.Features.PriceHistory.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/price-history")]
public class PriceHistoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public PriceHistoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductPriceHistoryDto>> GetPriceHistory(
        Guid productId,
        [FromQuery] int daysBack = 90)
    {
        // Clamp the untrusted look-back window so a huge/zero/negative value can't fetch all history or
        // overflow DateTime.AddDays (B-036 — anonymous public range param).
        var result = await _mediator.Send(new GetProductPriceHistoryQuery(productId, QueryBounds.Days(daysBack)));

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
