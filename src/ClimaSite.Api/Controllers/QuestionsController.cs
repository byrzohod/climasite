using ClimaSite.Application.Features.Questions.Commands;
using ClimaSite.Application.Features.Questions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get questions for a specific product
    /// </summary>
    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetProductQuestions(
        Guid productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeUnanswered = true)
    {
        var query = new GetProductQuestionsQuery
        {
            ProductId = productId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            IncludeUnanswered = includeUnanswered
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Ask a new question about a product.
    /// </summary>
    /// <remarks>
    /// This endpoint allows anonymous users to submit questions.
    /// TODO: Add rate limiting middleware to prevent spam (e.g., max 5 questions per IP per hour).
    /// Consider implementing: app.UseRateLimiter() with a sliding window policy for this endpoint.
    /// </remarks>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AskQuestion([FromBody] AskQuestionRequest request)
    {
        var command = new AskQuestionCommand
        {
            ProductId = request.ProductId,
            QuestionText = request.QuestionText,
            AskerName = request.AskerName,
            AskerEmail = request.AskerEmail
        };

        var questionId = await _mediator.Send(command);
        return CreatedAtAction(
            nameof(GetProductQuestions),
            new { productId = request.ProductId },
            new { id = questionId, message = "Question submitted for review" });
    }

    /// <summary>
    /// Submit an answer to a question
    /// </summary>
    [HttpPost("{questionId:guid}/answers")]
    public async Task<IActionResult> AnswerQuestion(
        Guid questionId,
        [FromBody] AnswerQuestionRequest request)
    {
        var command = new AnswerQuestionCommand
        {
            QuestionId = questionId,
            AnswerText = request.AnswerText,
            AnswererName = request.AnswererName,
            IsOfficial = false
        };

        var answerId = await _mediator.Send(command);
        return Ok(new { id = answerId, message = "Answer submitted for review" });
    }

    /// <summary>
    /// Submit an official answer (admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{questionId:guid}/official-answer")]
    public async Task<IActionResult> SubmitOfficialAnswer(
        Guid questionId,
        [FromBody] AnswerQuestionRequest request)
    {
        var command = new AnswerQuestionCommand
        {
            QuestionId = questionId,
            AnswerText = request.AnswerText,
            AnswererName = "ClimaSite Support",
            IsOfficial = true
        };

        var answerId = await _mediator.Send(command);
        return Ok(new { id = answerId, message = "Official answer submitted" });
    }

    /// <summary>
    /// Vote a question as helpful
    /// </summary>
    [HttpPost("{questionId:guid}/vote")]
    public async Task<IActionResult> VoteQuestion(Guid questionId)
    {
        var command = new VoteQuestionCommand { QuestionId = questionId };
        var helpfulCount = await _mediator.Send(command);
        return Ok(new { helpfulCount });
    }

    /// <summary>
    /// Vote an answer as helpful or unhelpful
    /// </summary>
    [HttpPost("answers/{answerId:guid}/vote")]
    public async Task<IActionResult> VoteAnswer(
        Guid answerId,
        [FromBody] VoteAnswerRequest request)
    {
        var command = new VoteAnswerCommand
        {
            AnswerId = answerId,
            IsHelpful = request.IsHelpful
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }
}

public record AskQuestionRequest
{
    public Guid ProductId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string? AskerName { get; init; }
    public string? AskerEmail { get; init; }
}

public record AnswerQuestionRequest
{
    public string AnswerText { get; init; } = string.Empty;
    public string? AnswererName { get; init; }
}

public record VoteAnswerRequest
{
    public bool IsHelpful { get; init; }
}
