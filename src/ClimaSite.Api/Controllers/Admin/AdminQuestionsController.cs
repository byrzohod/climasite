using ClimaSite.Application.Features.Questions.Commands;
using ClimaSite.Application.Features.Questions.Queries;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/questions")]
[Authorize(Roles = "Admin")]
public class AdminQuestionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminQuestionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all Q&A items pending moderation
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingModeration(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetPendingModerationQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get questions by status
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetQuestionsByStatus(
        [FromQuery] QuestionStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetPendingModerationQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            QuestionStatus = status
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Approve a question
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveQuestion(Guid id)
    {
        var command = new ModerateQuestionCommand
        {
            QuestionId = id,
            NewStatus = QuestionStatus.Approved
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true, message = "Question approved" });
    }

    /// <summary>
    /// Reject a question
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectQuestion(Guid id, [FromBody] ModerationNoteRequest? request = null)
    {
        var command = new ModerateQuestionCommand
        {
            QuestionId = id,
            NewStatus = QuestionStatus.Rejected,
            ModerationNote = request?.Note
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true, message = "Question rejected" });
    }

    /// <summary>
    /// Flag a question for review
    /// </summary>
    [HttpPost("{id:guid}/flag")]
    public async Task<IActionResult> FlagQuestion(Guid id, [FromBody] ModerationNoteRequest? request = null)
    {
        var command = new ModerateQuestionCommand
        {
            QuestionId = id,
            NewStatus = QuestionStatus.Flagged,
            ModerationNote = request?.Note
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true, message = "Question flagged for review" });
    }

    /// <summary>
    /// Approve an answer
    /// </summary>
    [HttpPost("answers/{id:guid}/approve")]
    public async Task<IActionResult> ApproveAnswer(Guid id, [FromBody] ApproveAnswerRequest? request = null)
    {
        var command = new ModerateAnswerCommand
        {
            AnswerId = id,
            NewStatus = AnswerStatus.Approved,
            MarkAsOfficial = request?.MarkAsOfficial
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true, message = "Answer approved" });
    }

    /// <summary>
    /// Reject an answer
    /// </summary>
    [HttpPost("answers/{id:guid}/reject")]
    public async Task<IActionResult> RejectAnswer(Guid id, [FromBody] ModerationNoteRequest? request = null)
    {
        var command = new ModerateAnswerCommand
        {
            AnswerId = id,
            NewStatus = AnswerStatus.Rejected,
            ModerationNote = request?.Note
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true, message = "Answer rejected" });
    }

    /// <summary>
    /// Flag an answer for review
    /// </summary>
    [HttpPost("answers/{id:guid}/flag")]
    public async Task<IActionResult> FlagAnswer(Guid id, [FromBody] ModerationNoteRequest? request = null)
    {
        var command = new ModerateAnswerCommand
        {
            AnswerId = id,
            NewStatus = AnswerStatus.Flagged,
            ModerationNote = request?.Note
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true, message = "Answer flagged for review" });
    }

    /// <summary>
    /// Bulk approve questions
    /// </summary>
    [HttpPost("bulk-approve")]
    public async Task<IActionResult> BulkApproveQuestions([FromBody] BulkModerationRequest request)
    {
        var approved = 0;
        var failed = 0;

        foreach (var id in request.Ids)
        {
            var command = new ModerateQuestionCommand
            {
                QuestionId = id,
                NewStatus = QuestionStatus.Approved
            };

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                approved++;
            else
                failed++;
        }

        return Ok(new { approved, failed });
    }

    /// <summary>
    /// Bulk approve answers
    /// </summary>
    [HttpPost("answers/bulk-approve")]
    public async Task<IActionResult> BulkApproveAnswers([FromBody] BulkModerationRequest request)
    {
        var approved = 0;
        var failed = 0;

        foreach (var id in request.Ids)
        {
            var command = new ModerateAnswerCommand
            {
                AnswerId = id,
                NewStatus = AnswerStatus.Approved
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

public record ModerationNoteRequest(string? Note);
public record ApproveAnswerRequest(bool? MarkAsOfficial);
public record BulkModerationRequest(List<Guid> Ids);
