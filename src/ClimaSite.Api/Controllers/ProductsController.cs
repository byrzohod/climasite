using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Application.Features.Products.Queries;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of products with optional filtering and sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ProductBriefDto>), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Get a product by its URL slug.
    /// </summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBySlug(string slug, [FromQuery] string? lang = null)
    {
        var query = new GetProductBySlugQuery { Slug = slug, LanguageCode = lang };
        var result = await _mediator.Send(query);
        if (result == null)
            return NotFound(new { message = "Product not found" });
        return Ok(result);
    }

    /// <summary>
    /// Get featured products for homepage display.
    /// </summary>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(List<ProductBriefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturedProducts(
        [FromQuery] int count = 8,
        [FromQuery] string? lang = null)
    {
        var query = new GetFeaturedProductsQuery { Count = count, LanguageCode = lang };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Search products by query string with optional filters.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PaginatedList<ProductBriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string q,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? categorySlug = null,
        [FromQuery] List<string>? brands = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? lang = null)
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
            MaxPrice = maxPrice,
            LanguageCode = lang
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get related products for a given product.
    /// </summary>
    [HttpGet("{id:guid}/related")]
    [ProducesResponseType(typeof(List<ProductBriefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRelatedProducts(
        Guid id,
        [FromQuery] int count = 8,
        [FromQuery] string? lang = null)
    {
        var query = new GetRelatedProductsQuery
        {
            ProductId = id,
            Count = count,
            LanguageCode = lang
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}/similar")]
    public async Task<IActionResult> GetSimilarProducts(
        Guid id,
        [FromQuery] int count = 8,
        [FromQuery] string? lang = null)
    {
        var query = new GetRelatedProductsQuery
        {
            ProductId = id,
            RelationType = RelationType.Similar,
            Count = count,
            LanguageCode = lang
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}/consumables")]
    public async Task<IActionResult> GetProductConsumables(
        Guid id,
        [FromQuery] int count = 6,
        [FromQuery] string? lang = null)
    {
        var query = new GetRelatedProductsQuery
        {
            ProductId = id,
            RelationType = RelationType.Accessory,
            Count = count,
            LanguageCode = lang
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}/frequently-bought-together")]
    public async Task<IActionResult> GetFrequentlyBoughtTogether(
        Guid id,
        [FromQuery] int count = 4,
        [FromQuery] string? lang = null)
    {
        var query = new GetRelatedProductsQuery
        {
            ProductId = id,
            RelationType = RelationType.FrequentlyBoughtTogether,
            Count = count,
            LanguageCode = lang
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get available filter options for products.
    /// </summary>
    [HttpGet("filters")]
    [ProducesResponseType(typeof(FilterOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilterOptions([FromQuery] string? categorySlug = null)
    {
        var query = new GetFilterOptionsQuery { CategorySlug = categorySlug };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
