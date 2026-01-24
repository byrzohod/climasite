using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Reviews.DTOs;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Reviews.Commands;

public record CreateReviewCommand : IRequest<Result<ReviewDto>>
{
    public Guid ProductId { get; init; }
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
}

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Content)
            .MaximumLength(5000).WithMessage("Content cannot exceed 5000 characters");
    }
}

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<ReviewDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateReviewCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ReviewDto>> Handle(
        CreateReviewCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<ReviewDto>.Failure("User must be authenticated to create a review");
        }

        // Check if product exists
        var productExists = await _context.Products
            .AnyAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (!productExists)
        {
            return Result<ReviewDto>.Failure("Product not found");
        }

        // Check if user already reviewed this product
        var existingReview = await _context.Reviews
            .AnyAsync(r => r.ProductId == request.ProductId && r.UserId == userId, cancellationToken);

        if (existingReview)
        {
            return Result<ReviewDto>.Failure("You have already reviewed this product");
        }

        var review = new Review(request.ProductId, userId.Value, request.Rating);
        review.SetTitle(request.Title);
        review.SetContent(request.Content);

        // TODO: Optimize this to use a single query instead of two
        // Could use FirstOrDefaultAsync directly and check for null
        // Check if user has purchased this product
        var order = await _context.Orders
            .Include(o => o.Items)
            .Where(o =>
                o.UserId == userId &&
                o.Status == OrderStatus.Delivered &&
                o.Items.Any(i => i.ProductId == request.ProductId))
            .FirstOrDefaultAsync(cancellationToken);

        if (order != null)
        {
            review.SetVerifiedPurchase(true, order.Id);
        }

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return Result<ReviewDto>.Success(new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserName = $"{user?.FirstName ?? "User"} {user?.LastName?.Substring(0, 1) ?? ""}.",
            Rating = review.Rating,
            Title = review.Title,
            Content = review.Content,
            Status = review.Status.ToString(),
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            HelpfulCount = review.HelpfulCount,
            UnhelpfulCount = review.UnhelpfulCount,
            AdminResponse = review.AdminResponse,
            AdminRespondedAt = review.AdminRespondedAt,
            CreatedAt = review.CreatedAt
        });
    }
}
