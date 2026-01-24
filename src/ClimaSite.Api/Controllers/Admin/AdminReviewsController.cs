using ClimaSite.Application.Features.Reviews.Commands;
using ClimaSite.Application.Features.Reviews.Queries;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all reviews pending moderation
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingReviews(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetPendingReviewsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get reviews by status
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReviewsByStatus(
        [FromQuery] ReviewStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetPendingReviewsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Approve a review
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveReview(Guid id)
    {
        var command = new ModerateReviewCommand
        {
            ReviewId = id,
            NewStatus = ReviewStatus.Approved
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true, message = "Review approved" });
    }

    /// <summary>
    /// Reject a review
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectReview(Guid id, [FromBody] ModerationNoteRequest? request = null)
    {
        var command = new ModerateReviewCommand
        {
            ReviewId = id,
            NewStatus = ReviewStatus.Rejected,
            ModerationNote = request?.Note
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true, message = "Review rejected" });
    }

    /// <summary>
    /// Flag a review for further review
    /// </summary>
    [HttpPost("{id:guid}/flag")]
    public async Task<IActionResult> FlagReview(Guid id, [FromBody] ModerationNoteRequest? request = null)
    {
        var command = new ModerateReviewCommand
        {
            ReviewId = id,
            NewStatus = ReviewStatus.Flagged,
            ModerationNote = request?.Note
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true, message = "Review flagged for review" });
    }

    /// <summary>
    /// Bulk approve reviews.
    /// </summary>
    /// <remarks>
    /// TODO: API-009 - This endpoint has an N+1 query problem. Each review moderation triggers
    /// a separate database call. For better performance with large datasets, implement a
    /// BulkModerateReviewsCommand that updates all reviews in a single transaction using
    /// ExecuteUpdateAsync or similar batch operation.
    /// </remarks>
    [HttpPost("bulk-approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkApproveReviews([FromBody] BulkModerationRequest request)
    {
        var approved = 0;
        var failed = 0;

        // TODO: Replace with batch update command for better performance
        foreach (var id in request.Ids)
        {
            var command = new ModerateReviewCommand
            {
                ReviewId = id,
                NewStatus = ReviewStatus.Approved
            };

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                approved++;
            else
                failed++;
        }

        return Ok(new { approved, failed });
    }
}

// Using the same records from AdminQuestionsController
