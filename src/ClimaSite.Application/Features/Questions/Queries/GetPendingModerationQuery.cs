using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Questions.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Questions.Queries;

public record GetPendingModerationQuery : IRequest<PendingModerationDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public QuestionStatus? QuestionStatus { get; init; }
    public AnswerStatus? AnswerStatus { get; init; }
}

public class GetPendingModerationQueryHandler : IRequestHandler<GetPendingModerationQuery, PendingModerationDto>
{
    private readonly IApplicationDbContext _context;

    public GetPendingModerationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PendingModerationDto> Handle(
        GetPendingModerationQuery request,
        CancellationToken cancellationToken)
    {
        // Get questions needing moderation
        var questionQuery = _context.ProductQuestions
            .AsNoTracking()
            .Include(q => q.Product)
            .Include(q => q.Answers)
            .Where(q => request.QuestionStatus.HasValue
                ? q.Status == request.QuestionStatus.Value
                : q.Status == QuestionStatus.Pending || q.Status == QuestionStatus.Flagged);

        var pendingQuestionsCount = await questionQuery.CountAsync(cancellationToken);

        var questions = await questionQuery
            .OrderByDescending(q => q.Status == QuestionStatus.Flagged)
            .ThenByDescending(q => q.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new AdminQuestionDto
            {
                Id = q.Id,
                ProductId = q.ProductId,
                ProductName = q.Product.Name,
                ProductSlug = q.Product.Slug,
                QuestionText = q.QuestionText,
                AskerName = q.AskerName,
                AskerEmail = q.AskerEmail,
                Status = q.Status,
                HelpfulCount = q.HelpfulCount,
                CreatedAt = q.CreatedAt,
                AnsweredAt = q.AnsweredAt,
                TotalAnswers = q.Answers.Count,
                PendingAnswers = q.Answers.Count(a => a.Status == Core.Entities.AnswerStatus.Pending)
            })
            .ToListAsync(cancellationToken);

        // Get answers needing moderation
        var answerQuery = _context.ProductAnswers
            .AsNoTracking()
            .Include(a => a.Question)
                .ThenInclude(q => q.Product)
            .Where(a => request.AnswerStatus.HasValue
                ? a.Status == request.AnswerStatus.Value
                : a.Status == Core.Entities.AnswerStatus.Pending || a.Status == Core.Entities.AnswerStatus.Flagged);

        var pendingAnswersCount = await answerQuery.CountAsync(cancellationToken);

        var answers = await answerQuery
            .OrderByDescending(a => a.Status == Core.Entities.AnswerStatus.Flagged)
            .ThenByDescending(a => a.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AdminAnswerDto
            {
                Id = a.Id,
                QuestionId = a.QuestionId,
                QuestionText = a.Question.QuestionText,
                ProductName = a.Question.Product.Name,
                ProductSlug = a.Question.Product.Slug,
                AnswerText = a.AnswerText,
                AnswererName = a.AnswererName,
                IsOfficial = a.IsOfficial,
                Status = a.Status,
                HelpfulCount = a.HelpfulCount,
                UnhelpfulCount = a.UnhelpfulCount,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PendingModerationDto
        {
            PendingQuestions = pendingQuestionsCount,
            PendingAnswers = pendingAnswersCount,
            Questions = questions,
            Answers = answers
        };
    }
}
