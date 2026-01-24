using ClimaSite.Application.Features.Categories.Commands;
using ClimaSite.Application.Features.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminCategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _mediator.Send(new GetCategoryTreeQuery());
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetCategory(string slug)
    {
        var query = new GetCategoryBySlugQuery { Slug = slug };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetCategory), new { slug = command.Name.ToLowerInvariant().Replace(" ", "-") }, new { id = result.Value });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { message = "Category ID mismatch" });
        }

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var command = new DeleteCategoryCommand { Id = id };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ToggleCategoryStatus(Guid id, [FromBody] ToggleCategoryStatusRequest request)
    {
        var command = new UpdateCategoryCommand
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

    /// <summary>
    /// Reorder categories by updating their sort order.
    /// </summary>
    /// <remarks>
    /// TODO: API-008 - This endpoint has an N+1 query problem. Each category update triggers
    /// a separate database call. For better performance with large datasets, implement a
    /// BatchUpdateCategorySortOrderCommand that updates all categories in a single transaction.
    /// </remarks>
    [HttpPatch("reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderCategories([FromBody] ReorderCategoriesRequest request)
    {
        // TODO: Replace with batch update command for better performance
        foreach (var item in request.Items)
        {
            var command = new UpdateCategoryCommand
            {
                Id = item.Id,
                SortOrder = item.SortOrder
            };
            await _mediator.Send(command);
        }

        return Ok(new { success = true });
    }
}

public record ToggleCategoryStatusRequest(bool IsActive);
public record ReorderCategoriesRequest(List<CategoryOrderItem> Items);
public record CategoryOrderItem(Guid Id, int SortOrder);
