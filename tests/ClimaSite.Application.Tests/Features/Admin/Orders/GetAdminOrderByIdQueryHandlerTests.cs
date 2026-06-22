using ClimaSite.Application.Features.Admin.Orders.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Orders;

public class GetAdminOrderByIdQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetAdminOrderByIdQueryHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_ExistingOrder_ReturnsDetailDto()
    {
        var order = new Order("ORD-DETAIL-0001", "buyer@test.com");
        order.SetCustomerPhone("+359888123456");
        _context.AddOrder(order);

        var result = await CreateHandler().Handle(
            new GetAdminOrderByIdQuery { Id = order.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.OrderNumber.Should().Be("ORD-DETAIL-0001");
        result.CustomerEmail.Should().Be("buyer@test.com");
        result.CustomerPhone.Should().Be("+359888123456");
        result.Status.Should().Be(nameof(OrderStatus.Pending));
        // Guest order: no User navigation -> CustomerName falls back to the email.
        result.CustomerName.Should().Be("buyer@test.com");
    }

    [Fact]
    public async Task Handle_OrderWithItems_MapsItemsAndImageUrl()
    {
        var product = new Product("SKU-AC-1", "Cool AC", "cool-ac", 599.99m);
        var image = new ProductImage(product.Id, "https://cdn.test/ac.jpg");
        image.SetPrimary(true);
        product.Images.Add(image);
        _context.AddProduct(product);

        var order = new Order("ORD-DETAIL-0002", "buyer@test.com");
        order.AddItem(product.Id, Guid.NewGuid(), "Cool AC", "Default", "SKU-AC-1", 2, 599.99m);
        _context.AddOrder(order);

        var result = await CreateHandler().Handle(
            new GetAdminOrderByIdQuery { Id = order.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        var item = result.Items[0];
        item.ProductName.Should().Be("Cool AC");
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(599.99m);
        item.ImageUrl.Should().Be("https://cdn.test/ac.jpg");
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNull()
    {
        var result = await CreateHandler().Handle(
            new GetAdminOrderByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }
}
