using ClimaSite.Application.Features.Promotions.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromotionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PromotionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetActivePromotions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? lang = null)
    {
        var query = new GetActivePromotionsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            LanguageCode = lang
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetPromotionBySlug(string slug, [FromQuery] string? lang = null)
    {
        var query = new GetPromotionBySlugQuery { Slug = slug, LanguageCode = lang };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { message = "Promotion not found" });

        return Ok(result);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedPromotions(
        [FromQuery] int count = 4,
        [FromQuery] string? lang = null)
    {
        var query = new GetFeaturedPromotionsQuery { Count = count, LanguageCode = lang };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
