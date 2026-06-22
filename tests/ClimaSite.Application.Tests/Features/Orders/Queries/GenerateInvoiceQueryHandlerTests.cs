using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Orders.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Orders.Queries;

public class GenerateInvoiceQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private GenerateInvoiceQueryHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    private (Order order, Product product) SeedOrder(Guid? userId)
    {
        var product = new Product("INV-001", "Invoiced AC", "invoiced-ac", 999.99m);
        var variant = new ProductVariant(product.Id, "INV-001-STD", "Standard");
        variant.SetStockQuantity(5);
        product.Variants.Add(variant);
        _context.AddProduct(product);

        var order = new Order("ORD-INVOICE1", "buyer@example.com");
        if (userId.HasValue)
        {
            order.SetUser(userId.Value);
        }
        order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, 2, 999.99m);
        _context.AddOrder(order);
        return (order, product);
    }

    [Fact]
    public async Task Handle_WhenOrderMissing_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(
            new GenerateInvoiceQuery { OrderId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Order not found");
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnOrderAndNotAdmin_ReturnsAccessDenied()
    {
        var (order, _) = SeedOrder(userId: Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);

        var result = await CreateHandler().Handle(
            new GenerateInvoiceQuery { OrderId = order.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_WhenUserOwnsOrder_GeneratesPdf()
    {
        var userId = Guid.NewGuid();
        var (order, _) = SeedOrder(userId);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(false);

        var result = await CreateHandler().Handle(
            new GenerateInvoiceQuery { OrderId = order.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PdfContent.Should().NotBeEmpty();
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.FileName.Should().Be($"Invoice-{order.OrderNumber}.pdf");
    }

    [Fact]
    public async Task Handle_WhenAdmin_CanGenerateForAnotherUsersOrder()
    {
        var (order, _) = SeedOrder(userId: Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(true);

        var result = await CreateHandler().Handle(
            new GenerateInvoiceQuery { OrderId = order.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PdfContent.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_AnonymousCaller_CanGenerateWhenOrderResolves()
    {
        var (order, _) = SeedOrder(userId: null);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new GenerateInvoiceQuery { OrderId = order.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FileName.Should().Contain(order.OrderNumber);
    }
}
