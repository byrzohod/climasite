using ClimaSite.Application.Features.Categories.DTOs;
using ClimaSite.Application.Features.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get category tree with optional filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryTreeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryTree(
        [FromQuery] string? name = null,
        [FromQuery] string? lang = null)
    {
        var query = new GetCategoryTreeQuery { NameFilter = name, LanguageCode = lang };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a category by its URL slug.
    /// </summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryBySlug(
        string slug,
        [FromQuery] string? lang = null)
    {
        var query = new GetCategoryBySlugQuery { Slug = slug, LanguageCode = lang };
        var result = await _mediator.Send(query);
        if (result == null)
            return NotFound(new { message = "Category not found" });
        return Ok(result);
    }
}
