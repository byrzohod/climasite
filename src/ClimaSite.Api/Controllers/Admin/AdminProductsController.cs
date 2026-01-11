using ClimaSite.Application.Features.Products.Commands;
using ClimaSite.Application.Features.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? brand = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        var query = new GetProductsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            CategoryId = categoryId,
            SearchTerm = searchTerm,
            Brand = brand,
            IsFeatured = null,
            InStock = null,
            OnSale = null,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetProduct(string slug)
    {
        var query = new GetProductBySlugQuery { Slug = slug };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        // Generate slug for response (same logic as in handler)
        var slug = command.Name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
        var chars = slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray();
        slug = new string(chars);
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }
        slug = slug.Trim('-');

        return CreatedAtAction(nameof(GetProduct), new { slug }, new { id = result.Value, slug });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { message = "Product ID mismatch" });
        }

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var command = new DeleteProductCommand { Id = id };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ToggleProductStatus(Guid id, [FromBody] ToggleStatusRequest request)
    {
        var command = new UpdateProductCommand
        {
            Id = id,
            IsActive = request.IsActive
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true });
    }

    [HttpPatch("{id:guid}/featured")]
    public async Task<IActionResult> ToggleProductFeatured(Guid id, [FromBody] ToggleFeaturedRequest request)
    {
        var command = new UpdateProductCommand
        {
            Id = id,
            IsFeatured = request.IsFeatured
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true });
    }
}

public record ToggleStatusRequest(bool IsActive);
public record ToggleFeaturedRequest(bool IsFeatured);
