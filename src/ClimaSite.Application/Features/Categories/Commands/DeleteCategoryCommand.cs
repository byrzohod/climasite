using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Categories.Commands;

public record DeleteCategoryCommand : IRequest<Result<bool>>
{
    public Guid Id { get; init; }
}

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync(new object[] { request.Id }, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", request.Id);
        }

        var hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == request.Id, cancellationToken);
        if (hasChildren)
        {
            return Result<bool>.Failure("Cannot delete a category that has child categories. Please delete or move the child categories first.");
        }

        var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == request.Id, cancellationToken);
        if (hasProducts)
        {
            return Result<bool>.Failure("Cannot delete a category that has products. Please move the products to another category first.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
