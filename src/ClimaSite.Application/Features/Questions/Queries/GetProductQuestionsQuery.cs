using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Questions.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Questions.Queries;

public record GetProductQuestionsQuery : IRequest<ProductQuestionsDto>
{
    public Guid ProductId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool IncludeUnanswered { get; init; } = true;
}

public class GetProductQuestionsQueryHandler : IRequestHandler<GetProductQuestionsQuery, ProductQuestionsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProductQuestionsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProductQuestionsDto> Handle(
        GetProductQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.ProductQuestions
            .AsNoTracking()
            .Include(q => q.Answers.Where(a => a.Status == AnswerStatus.Approved))
            .Where(q => q.ProductId == request.ProductId && q.Status == QuestionStatus.Approved);

        // Answered-state is derived from APPROVED-answer existence, never from the denormalized AnsweredAt
        // column — so this read heals any legacy/racy AnsweredAt drift (B-038): a question counts/filters as
        // answered iff it actually has an approved answer.
        if (!request.IncludeUnanswered)
        {
            query = query.Where(q => q.Answers.Any(a => a.Status == AnswerStatus.Approved));
        }

        var totalQuestions = await query.CountAsync(cancellationToken);
        var answeredQuestions = await query.CountAsync(
            q => q.Answers.Any(a => a.Status == AnswerStatus.Approved), cancellationToken);

        var questions = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Batch-load the current user's own votes for the page (no per-row join / N+1). Anonymous
        // callers get an empty lookup, so every question/answer surfaces as not-yet-voted. (B-039)
        var votedQuestionIds = new HashSet<Guid>();
        var answerVotes = new Dictionary<Guid, bool>();
        var userId = _currentUserService.UserId;
        if (userId.HasValue && questions.Count > 0)
        {
            var questionIds = questions.Select(q => q.Id).ToList();
            var answerIds = questions.SelectMany(q => q.Answers.Select(a => a.Id)).ToList();

            votedQuestionIds = (await _context.ProductQuestionVotes
                .AsNoTracking()
                .Where(v => v.UserId == userId.Value && questionIds.Contains(v.QuestionId))
                .Select(v => v.QuestionId)
                .ToListAsync(cancellationToken)).ToHashSet();

            if (answerIds.Count > 0)
            {
                answerVotes = await _context.ProductAnswerVotes
                    .AsNoTracking()
                    .Where(v => v.UserId == userId.Value && answerIds.Contains(v.AnswerId))
                    .ToDictionaryAsync(v => v.AnswerId, v => v.IsHelpful, cancellationToken);
            }
        }

        return new ProductQuestionsDto
        {
            ProductId = request.ProductId,
            TotalQuestions = totalQuestions,
            AnsweredQuestions = answeredQuestions,
            Questions = questions.Select(q => new QuestionDto
            {
                Id = q.Id,
                ProductId = q.ProductId,
                QuestionText = q.QuestionText,
                AskerName = q.AskerName ?? "Anonymous",
                HelpfulCount = q.HelpfulCount,
                HasVotedHelpful = votedQuestionIds.Contains(q.Id),
                CreatedAt = q.CreatedAt,
                // Expose the answered timestamp only when an approved answer actually exists, so the DTO is
                // internally consistent even if the persisted AnsweredAt drifted (B-038).
                AnsweredAt = q.Answers.Any(a => a.Status == AnswerStatus.Approved) ? q.AnsweredAt : null,
                AnswerCount = q.Answers.Count(a => a.Status == AnswerStatus.Approved),
                Answers = q.Answers
                    .Where(a => a.Status == AnswerStatus.Approved)
                    .OrderByDescending(a => a.IsOfficial)
                    .ThenByDescending(a => a.HelpfulCount)
                    .ThenBy(a => a.CreatedAt)
                    .Select(a => new AnswerDto
                    {
                        Id = a.Id,
                        QuestionId = a.QuestionId,
                        AnswerText = a.AnswerText,
                        AnswererName = a.AnswererName ?? (a.IsOfficial ? "ClimaSite Support" : "Community Member"),
                        IsOfficial = a.IsOfficial,
                        HelpfulCount = a.HelpfulCount,
                        UnhelpfulCount = a.UnhelpfulCount,
                        UserVoteHelpful = answerVotes.TryGetValue(a.Id, out var isHelpful) ? isHelpful : null,
                        CreatedAt = a.CreatedAt
                    }).ToList()
            }).ToList()
        };
    }
}
