using ClimaSite.Application.Features.Brands.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BrandsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetBrands(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] bool? featured = null,
        [FromQuery] string? lang = null)
    {
        var query = new GetBrandsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            FeaturedOnly = featured,
            LanguageCode = lang
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBrandBySlug(
        string slug,
        [FromQuery] int productPage = 1,
        [FromQuery] int productPageSize = 12,
        [FromQuery] string? lang = null)
    {
        var query = new GetBrandBySlugQuery
        {
            Slug = slug,
            ProductPageNumber = productPage,
            ProductPageSize = productPageSize,
            LanguageCode = lang
        };

        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { message = "Brand not found" });

        return Ok(result);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedBrands(
        [FromQuery] int limit = 8,
        [FromQuery] string? lang = null)
    {
        var query = new GetFeaturedBrandsQuery { Limit = limit, LanguageCode = lang };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
