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

    public GetProductQuestionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
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
                        CreatedAt = a.CreatedAt
                    }).ToList()
            }).ToList()
        };
    }
}
