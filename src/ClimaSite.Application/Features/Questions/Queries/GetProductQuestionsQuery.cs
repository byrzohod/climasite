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

        if (!request.IncludeUnanswered)
        {
            query = query.Where(q => q.AnsweredAt.HasValue);
        }

        var totalQuestions = await query.CountAsync(cancellationToken);
        var answeredQuestions = await query.CountAsync(q => q.AnsweredAt.HasValue, cancellationToken);

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
                AnsweredAt = q.AnsweredAt,
                AnswerCount = q.Answers.Count,
                Answers = q.Answers
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
