using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Cart.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.ShoppingCart.Commands;

public class AddToCartCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private AddToCartCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    private static Product CreateProduct(
        string sku = "AC-001",
        decimal basePrice = 500m,
        int stock = 10,
        bool active = true,
        bool variantActive = true)
    {
        var product = new Product(sku, $"{sku} AC", $"{sku.ToLower()}-ac", basePrice);
        product.SetActive(active);
        var variant = new ProductVariant(product.Id, $"{sku}-STD", "Standard");
        variant.SetStockQuantity(stock);
        variant.SetActive(variantActive);
        product.Variants.Add(variant);
        return product;
    }

    [Fact]
    public async Task Handle_WhenProductMissing_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new AddToCartCommand { ProductId = Guid.NewGuid(), Quantity = 1, GuestSessionId = "guest-1" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Product not found");
    }

    [Fact]
    public async Task Handle_WhenVariantSpecifiedButInactive_ReturnsFailure()
    {
        var product = CreateProduct(variantActive: false);
        _context.AddProduct(product);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new AddToCartCommand
            {
                ProductId = product.Id,
                VariantId = product.Variants.First().Id,
                Quantity = 1,
                GuestSessionId = "guest-1"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("variant not found");
    }

    [Fact]
    public async Task Handle_WhenStockInsufficient_ReturnsFailure()
    {
        var product = CreateProduct(stock: 2);
        _context.AddProduct(product);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new AddToCartCommand { ProductId = product.Id, Quantity = 5, GuestSessionId = "guest-1" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only 2 items available");
    }

    [Fact]
    public async Task Handle_CreatesGuestCart_AndAddsItem()
    {
        var product = CreateProduct(basePrice: 500m, stock: 10);
        _context.AddProduct(product);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new AddToCartCommand { ProductId = product.Id, Quantity = 2, GuestSessionId = "guest-42" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemCount.Should().Be(2);
        result.Value.Items.Should().ContainSingle();
        result.Value.Subtotal.Should().Be(1000m);
        result.Value.Tax.Should().Be(200m, "20% VAT");
        result.Value.Total.Should().Be(1200m);

        var savedCart = await _context.Carts.Include(c => c.Items).SingleAsync();
        savedCart.SessionId.Should().Be("guest-42");
        savedCart.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WhenItemAlreadyInCart_AccumulatesQuantity()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 10);
        var variantId = product.Variants.First().Id;
        _context.AddProduct(product);

        var cart = new Core.Entities.Cart(userId, null);
        cart.AddItem(product.Id, variantId, 1, 500m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new AddToCartCommand { ProductId = product.Id, VariantId = variantId, Quantity = 3 },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemCount.Should().Be(4, "1 existing + 3 added");
        result.Value.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WhenAccumulatedQuantityExceedsStock_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 5);
        var variantId = product.Variants.First().Id;
        _context.AddProduct(product);

        var cart = new Core.Entities.Cart(userId, null);
        cart.AddItem(product.Id, variantId, 4, 500m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new AddToCartCommand { ProductId = product.Id, VariantId = variantId, Quantity = 3 },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only 5 available");
    }
}
