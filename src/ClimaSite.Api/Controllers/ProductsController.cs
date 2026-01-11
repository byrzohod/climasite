using ClimaSite.Application.Features.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? brand = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? inStock = null,
        [FromQuery] bool? onSale = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        [FromQuery] string? lang = null)
    {
        var query = new GetProductsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            CategoryId = categoryId,
            SearchTerm = searchTerm,
            Brand = brand,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            InStock = inStock,
            OnSale = onSale,
            IsFeatured = isFeatured,
            SortBy = sortBy,
            SortDescending = sortDescending,
            LanguageCode = lang
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetProductBySlug(string slug, [FromQuery] string? lang = null)
    {
        var query = new GetProductBySlugQuery { Slug = slug, LanguageCode = lang };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedProducts([FromQuery] int count = 8)
    {
        var query = new GetFeaturedProductsQuery { Count = count };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string q,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? categorySlug = null,
        [FromQuery] List<string>? brands = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { message = "Search query is required" });
        }

        var query = new SearchProductsQuery
        {
            Query = q,
            PageNumber = pageNumber,
            PageSize = pageSize,
            CategorySlug = categorySlug,
            Brands = brands,
            MinPrice = minPrice,
            MaxPrice = maxPrice
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}/related")]
    public async Task<IActionResult> GetRelatedProducts(Guid id, [FromQuery] int count = 8)
    {
        var query = new GetRelatedProductsQuery
        {
            ProductId = id,
            Count = count
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("filters")]
    public async Task<IActionResult> GetFilterOptions([FromQuery] string? categorySlug = null)
    {
        var query = new GetFilterOptionsQuery { CategorySlug = categorySlug };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
