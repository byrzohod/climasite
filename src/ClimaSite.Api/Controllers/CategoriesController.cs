using ClimaSite.Application.Features.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategoryTree([FromQuery] string? name = null)
    {
        var query = new GetCategoryTreeQuery { NameFilter = name };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetCategoryBySlug(string slug)
    {
        var query = new GetCategoryBySlugQuery { Slug = slug };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
