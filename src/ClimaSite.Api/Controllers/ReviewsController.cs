using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Reviews.Commands;
using ClimaSite.Application.Features.Reviews.DTOs;
using ClimaSite.Application.Features.Reviews.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated reviews for a product.
    /// </summary>
    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PaginatedList<ReviewDto>), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Get review summary (ratings distribution) for a product.
    /// </summary>
    [HttpGet("product/{productId:guid}/summary")]
    [ProducesResponseType(typeof(ProductReviewSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductReviewSummary(Guid productId)
    {
        var query = new GetProductReviewSummaryQuery { ProductId = productId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new review for a product.
    /// </summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Mark a review as helpful.
    /// </summary>
    /// <param name="reviewId">The ID of the review to vote on.</param>
    /// <returns>Updated vote counts for the review.</returns>
    [Authorize]
    [HttpPost("{reviewId:guid}/helpful")]
    [ProducesResponseType(typeof(VoteReviewResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkReviewHelpful(Guid reviewId)
    {
        var command = new VoteReviewCommand
        {
            ReviewId = reviewId,
            IsHelpful = true
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
                return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }

        return Ok(new VoteReviewResultDto
        {
            HelpfulCount = result.Value!.HelpfulCount,
            UnhelpfulCount = result.Value.UnhelpfulCount,
            UserVotedHelpful = result.Value.UserVotedHelpful
        });
    }

    /// <summary>
    /// Mark a review as unhelpful.
    /// </summary>
    /// <param name="reviewId">The ID of the review to vote on.</param>
    /// <returns>Updated vote counts for the review.</returns>
    [Authorize]
    [HttpPost("{reviewId:guid}/unhelpful")]
    [ProducesResponseType(typeof(VoteReviewResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkReviewUnhelpful(Guid reviewId)
    {
        var command = new VoteReviewCommand
        {
            ReviewId = reviewId,
            IsHelpful = false
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
                return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }

        return Ok(new VoteReviewResultDto
        {
            HelpfulCount = result.Value!.HelpfulCount,
            UnhelpfulCount = result.Value.UnhelpfulCount,
            UserVotedHelpful = result.Value.UserVotedHelpful
        });
    }
}

/// <summary>
/// Response DTO for review vote operations.
/// </summary>
public record VoteReviewResultDto
{
    public int HelpfulCount { get; init; }
    public int UnhelpfulCount { get; init; }
    public bool UserVotedHelpful { get; init; }
}
