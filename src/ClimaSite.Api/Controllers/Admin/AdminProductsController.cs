using ClimaSite.Application.Features.Products.Commands;
using ClimaSite.Application.Features.Products.Queries;
using ClimaSite.Application.Features.Admin.RelatedProducts.Commands;
using ClimaSite.Application.Features.Admin.RelatedProducts.Queries;
using ClimaSite.Application.Features.Admin.Translations.Commands;
using ClimaSite.Application.Features.Admin.Translations.Queries;
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

        // TODO: Consider returning the slug from the command handler to avoid duplicating slug generation logic
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

    // Related Products Management

    [HttpGet("{id:guid}/relations")]
    public async Task<IActionResult> GetProductRelations(Guid id)
    {
        var query = new GetProductRelationsQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("{id:guid}/relations")]
    public async Task<IActionResult> AddRelatedProduct(Guid id, [FromBody] AddRelationRequest request)
    {
        var command = new AddRelatedProductCommand(id, request.RelatedProductId, request.RelationType);
        var relationId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProductRelations), new { id }, new { relationId });
    }

    [HttpDelete("{id:guid}/relations/{relationId:guid}")]
    public async Task<IActionResult> RemoveRelatedProduct(Guid id, Guid relationId)
    {
        var command = new RemoveRelatedProductCommand(id, relationId);
        var success = await _mediator.Send(command);

        if (!success)
        {
            return NotFound();
        }

        return Ok(new { success = true });
    }

    [HttpPut("{id:guid}/relations/reorder")]
    public async Task<IActionResult> ReorderRelatedProducts(Guid id, [FromBody] ReorderRelationsRequest request)
    {
        var command = new ReorderRelatedProductsCommand(id, request.RelationType, request.RelationIds);
        var success = await _mediator.Send(command);

        if (!success)
        {
            return BadRequest(new { message = "Failed to reorder relations" });
        }

        return Ok(new { success = true });
    }


    // Translation Management

    [HttpGet("{id:guid}/translations")]
    public async Task<IActionResult> GetProductTranslations(Guid id)
    {
        var query = new GetProductTranslationsQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("{id:guid}/translations")]
    public async Task<IActionResult> AddProductTranslation(Guid id, [FromBody] AddTranslationRequest request)
    {
        var command = new AddProductTranslationCommand
        {
            ProductId = id,
            LanguageCode = request.LanguageCode,
            Name = request.Name,
            ShortDescription = request.ShortDescription,
            Description = request.Description,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription
        };

        var translationId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProductTranslations), new { id }, new { translationId });
    }

    [HttpPut("{id:guid}/translations/{languageCode}")]
    public async Task<IActionResult> UpdateProductTranslation(
        Guid id,
        string languageCode,
        [FromBody] UpdateTranslationRequest request)
    {
        var command = new UpdateProductTranslationCommand
        {
            ProductId = id,
            LanguageCode = languageCode,
            Name = request.Name,
            ShortDescription = request.ShortDescription,
            Description = request.Description,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription
        };

        var success = await _mediator.Send(command);

        if (!success)
        {
            return NotFound(new { message = $"Translation for language '{languageCode}' not found" });
        }

        return Ok(new { success = true });
    }

    [HttpDelete("{id:guid}/translations/{languageCode}")]
    public async Task<IActionResult> DeleteProductTranslation(Guid id, string languageCode)
    {
        var command = new DeleteProductTranslationCommand(id, languageCode);
        var success = await _mediator.Send(command);

        if (!success)
        {
            return NotFound(new { message = $"Translation for language '{languageCode}' not found" });
        }

        return Ok(new { success = true });
    }
}

public record ToggleStatusRequest(bool IsActive);
public record ToggleFeaturedRequest(bool IsFeatured);
public record AddRelationRequest(Guid RelatedProductId, string RelationType);
public record ReorderRelationsRequest(string RelationType, List<Guid> RelationIds);
public record AddTranslationRequest(
    string LanguageCode,
    string Name,
    string? ShortDescription,
    string? Description,
    string? MetaTitle,
    string? MetaDescription);
public record UpdateTranslationRequest(
    string Name,
    string? ShortDescription,
    string? Description,
    string? MetaTitle,
    string? MetaDescription);
