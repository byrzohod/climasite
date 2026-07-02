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
    public async Task Handle_WhenReservationsLeaveInsufficientStock_ReturnsFailure()
    {
        // INV-01 A3: 5 in stock but 4 are held by an in-flight checkout -> only 1 truly available.
        var product = CreateProduct(stock: 5);
        product.Variants.First().SetReservedQuantity(4);
        _context.AddProduct(product);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new AddToCartCommand { ProductId = product.Id, Quantity = 3, GuestSessionId = "guest-1" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only 1 items available");
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

    [Fact]
    public async Task Handle_WhenAccumulatedQuantityExceedsReservationAdjustedStock_ReturnsFailure()
    {
        // INV-01 A3: the accumulated-quantity ceiling is also reservation-aware — stock 6, reserved 2 =>
        // only 4 available; 1 already in cart + 4 more would exceed it.
        var userId = Guid.NewGuid();
        var product = CreateProduct(stock: 6);
        product.Variants.First().SetReservedQuantity(2);
        var variantId = product.Variants.First().Id;
        _context.AddProduct(product);

        var cart = new Core.Entities.Cart(userId, null);
        cart.AddItem(product.Id, variantId, 1, 500m);
        _context.AddCart(cart);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new AddToCartCommand { ProductId = product.Id, VariantId = variantId, Quantity = 4 },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only 4 available");
    }

    [Fact]
    public async Task Handle_NoVariantSpecified_PicksFirstActiveVariantWithAvailability()
    {
        // INV-01 A3 [Medium fix]: with no explicit variant, the first active variant is fully reserved
        // (available 0) but a later one has stock -> add must succeed against the available variant, so the
        // aggregate "in stock" the PDP shows agrees with add-to-cart.
        var product = new Product("MV-001", "Multi Variant AC", "multi-variant-ac", 500m);
        product.SetActive(true);
        var reserved = new ProductVariant(product.Id, "MV-001-A", "First");
        reserved.SetStockQuantity(5);
        reserved.SetReservedQuantity(5); // available 0
        var available = new ProductVariant(product.Id, "MV-001-B", "Second");
        available.SetStockQuantity(5); // available 5
        product.Variants.Add(reserved);
        product.Variants.Add(available);
        _context.AddProduct(product);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new AddToCartCommand { ProductId = product.Id, Quantity = 2, GuestSessionId = "guest-mv" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items[0].VariantId.Should().Be(available.Id, "the available variant must be chosen");
    }

    [Fact]
    public async Task Handle_NoVariantSpecified_AllVariantsFullyReserved_ReturnsFailure()
    {
        // Fallback path: when no active variant has availability, selection falls back to the first active
        // variant so the availability check produces the honest "Only 0 items available" failure.
        var product = new Product("MV-002", "Sold Out AC", "sold-out-ac", 500m);
        product.SetActive(true);
        var v1 = new ProductVariant(product.Id, "MV-002-A", "First");
        v1.SetStockQuantity(3);
        v1.SetReservedQuantity(3);
        var v2 = new ProductVariant(product.Id, "MV-002-B", "Second");
        v2.SetStockQuantity(2);
        v2.SetReservedQuantity(2);
        product.Variants.Add(v1);
        product.Variants.Add(v2);
        _context.AddProduct(product);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new AddToCartCommand { ProductId = product.Id, Quantity = 1, GuestSessionId = "guest-mv2" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only 0 items available");
    }
}
