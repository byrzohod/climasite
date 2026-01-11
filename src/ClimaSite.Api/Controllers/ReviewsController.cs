using ClimaSite.Application.Features.Reviews.Commands;
using ClimaSite.Application.Features.Reviews.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetProductReviews(
        Guid productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "newest")
    {
        var query = new GetProductReviewsQuery
        {
            ProductId = productId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("product/{productId:guid}/summary")]
    public async Task<IActionResult> GetProductReviewSummary(Guid productId)
    {
        var query = new GetProductReviewSummaryQuery { ProductId = productId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(
            nameof(GetProductReviews),
            new { productId = result.Value!.ProductId },
            result.Value);
    }

    [Authorize]
    [HttpPost("{reviewId:guid}/helpful")]
    public async Task<IActionResult> MarkReviewHelpful(Guid reviewId)
    {
        // TODO: Implement vote helpful
        return Ok(new { message = "Vote recorded" });
    }

    [Authorize]
    [HttpPost("{reviewId:guid}/unhelpful")]
    public async Task<IActionResult> MarkReviewUnhelpful(Guid reviewId)
    {
        // TODO: Implement vote unhelpful
        return Ok(new { message = "Vote recorded" });
    }
}
