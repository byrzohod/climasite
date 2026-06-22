using ClimaSite.Application.Features.Admin.Products.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Admin.Products;

public class DeleteProductCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private DeleteProductCommandHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_ExistingProduct_SoftDeletesByDeactivating()
    {
        var product = new Product("AC-DEL", "Deletable", "deletable", 100m);
        product.SetActive(true);
        _context.AddProduct(product);

        var result = await CreateHandler().Handle(
            new DeleteProductCommand { Id = product.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Soft delete: the product is deactivated, not removed.
        product.IsActive.Should().BeFalse();
        (await _context.Products.ToListAsync()).Should().ContainSingle(p => p.Id == product.Id);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        var result = await CreateHandler().Handle(
            new DeleteProductCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }
}
