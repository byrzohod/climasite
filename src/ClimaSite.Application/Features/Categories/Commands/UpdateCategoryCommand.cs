using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Categories.Commands;

public record UpdateCategoryCommand : IRequest<Result<bool>>
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
    public string? ImageUrl { get; init; }
    public string? Icon { get; init; }
    public int? SortOrder { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public bool? IsActive { get; init; }
}

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Category ID is required");

        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL cannot exceed 500 characters")
            .When(x => x.ImageUrl != null);

        RuleFor(x => x.Icon)
            .MaximumLength(50).WithMessage("Icon cannot exceed 50 characters")
            .When(x => x.Icon != null);

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta title cannot exceed 200 characters")
            .When(x => x.MetaTitle != null);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta description cannot exceed 500 characters")
            .When(x => x.MetaDescription != null);
    }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync(new object[] { request.Id }, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", request.Id);
        }

        if (request.ParentId.HasValue && request.ParentId.Value != category.ParentId)
        {
            if (request.ParentId.Value == request.Id)
            {
                return Result<bool>.Failure("A category cannot be its own parent");
            }

            var parentExists = await _context.Categories.AnyAsync(c => c.Id == request.ParentId.Value, cancellationToken);
            if (!parentExists)
            {
                return Result<bool>.Failure($"Parent category with ID '{request.ParentId}' not found");
            }

            if (await IsDescendantOfAsync(request.ParentId.Value, request.Id, cancellationToken))
            {
                return Result<bool>.Failure("Cannot set a descendant category as parent (circular reference)");
            }
        }

        if (request.Name != null)
        {
            category.SetName(request.Name);
        }

        if (request.Description != null)
        {
            category.SetDescription(request.Description);
        }

        if (request.ParentId.HasValue)
        {
            category.SetParent(request.ParentId);
        }

        if (request.ImageUrl != null)
        {
            category.SetImageUrl(request.ImageUrl);
        }

        if (request.Icon != null)
        {
            category.SetIcon(request.Icon);
        }

        if (request.SortOrder.HasValue)
        {
            category.SetSortOrder(request.SortOrder.Value);
        }

        if (request.MetaTitle != null)
        {
            category.SetMetaTitle(request.MetaTitle);
        }

        if (request.MetaDescription != null)
        {
            category.SetMetaDescription(request.MetaDescription);
        }

        if (request.IsActive.HasValue)
        {
            category.SetActive(request.IsActive.Value);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    private async Task<bool> IsDescendantOfAsync(Guid potentialParentId, Guid categoryId, CancellationToken cancellationToken)
    {
        var current = await _context.Categories.FindAsync(new object[] { potentialParentId }, cancellationToken);

        while (current?.ParentId != null)
        {
            if (current.ParentId == categoryId)
            {
                return true;
            }

            current = await _context.Categories.FindAsync(new object[] { current.ParentId }, cancellationToken);
        }

        return false;
    }
}
