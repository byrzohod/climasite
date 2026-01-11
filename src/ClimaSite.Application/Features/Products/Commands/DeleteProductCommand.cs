using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using MediatR;

namespace ClimaSite.Application.Features.Products.Commands;

public record DeleteProductCommand : IRequest<Result<bool>>
{
    public Guid Id { get; init; }
}

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync(new object[] { request.Id }, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        product.SetActive(false);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
