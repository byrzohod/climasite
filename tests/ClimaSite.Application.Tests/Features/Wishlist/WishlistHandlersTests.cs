using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Wishlist.Commands;
using ClimaSite.Application.Features.Wishlist.Queries;
using ClimaSite.Application.Features.Wishlist.Services;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClimaSite.Application.Tests.Features.Wishlist;

public class WishlistHandlersTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    [Fact]
    public async Task AddToWishlist_CreatesWishlistAndReturnsProductDto()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct("WISH-001", "Wishlist Test AC", "wishlist-test-ac");
        _context.AddProduct(product);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var handler = new AddToWishlistCommandHandler(
            _context,
            _currentUserServiceMock.Object,
            CreateWishlistService());

        var result = await handler.Handle(
            new AddToWishlistCommand { ProductId = product.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(userId);
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].ProductId.Should().Be(product.Id);
        result.Value.Items[0].ProductName.Should().Be("Wishlist Test AC");
        result.Value.Items[0].PrimaryImageUrl.Should().Be("/images/wishlist-test-ac.webp");
        result.Value.Items[0].InStock.Should().BeTrue();
    }

    [Fact]
    public async Task AddToWishlist_DoesNotDuplicateExistingItem()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct("WISH-002", "Duplicate Test AC", "duplicate-test-ac");
        var wishlist = new Core.Entities.Wishlist(userId);
        wishlist.AddItem(product.Id);
        _context.AddProduct(product);
        _context.AddWishlist(wishlist);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var handler = new AddToWishlistCommandHandler(
            _context,
            _currentUserServiceMock.Object,
            CreateWishlistService());

        var result = await handler.Handle(
            new AddToWishlistCommand { ProductId = product.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        wishlist.Items.Should().ContainSingle();
    }

    [Fact]
    public void IsUniqueViolation_ReturnsTrue_ForPostgresUniqueViolationSqlState()
    {
        var exception = new DbUpdateException(
            "duplicate key",
            new FakePostgresException("23505"));

        WishlistApplicationService.IsUniqueViolation(exception).Should().BeTrue();
    }

    [Fact]
    public void IsUniqueViolation_ReturnsFalse_ForOtherSqlState()
    {
        var exception = new DbUpdateException(
            "some other failure",
            new FakePostgresException("23503"));

        WishlistApplicationService.IsUniqueViolation(exception).Should().BeFalse();
    }

    [Fact]
    public void IsUniqueViolation_ReturnsFalse_WhenNoSqlStatePresent()
    {
        var exception = new DbUpdateException("generic", new InvalidOperationException("boom"));

        WishlistApplicationService.IsUniqueViolation(exception).Should().BeFalse();
    }

    [Fact]
    public async Task AddToWishlist_ReturnsFailure_WhenProductIsInactive()
    {
        var product = CreateProduct("WISH-003", "Inactive AC", "inactive-ac");
        product.SetActive(false);
        _context.AddProduct(product);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var handler = new AddToWishlistCommandHandler(
            _context,
            _currentUserServiceMock.Object,
            CreateWishlistService());

        var result = await handler.Handle(
            new AddToWishlistCommand { ProductId = product.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Fact]
    public async Task RemoveFromWishlist_RemovesExistingItem()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct("WISH-004", "Remove Test AC", "remove-test-ac");
        var wishlist = new Core.Entities.Wishlist(userId);
        wishlist.AddItem(product.Id);
        _context.AddProduct(product);
        _context.AddWishlist(wishlist);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var handler = new RemoveFromWishlistCommandHandler(
            _context,
            _currentUserServiceMock.Object,
            CreateWishlistService());

        var result = await handler.Handle(
            new RemoveFromWishlistCommand { ProductId = product.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        wishlist.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearWishlist_RemovesAllItems()
    {
        var userId = Guid.NewGuid();
        var productOne = CreateProduct("WISH-005", "Clear Test AC 1", "clear-test-ac-1");
        var productTwo = CreateProduct("WISH-006", "Clear Test AC 2", "clear-test-ac-2");
        var wishlist = new Core.Entities.Wishlist(userId);
        wishlist.AddItem(productOne.Id);
        wishlist.AddItem(productTwo.Id);
        _context.AddProduct(productOne);
        _context.AddProduct(productTwo);
        _context.AddWishlist(wishlist);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var handler = new ClearWishlistCommandHandler(
            _context,
            _currentUserServiceMock.Object,
            CreateWishlistService());

        var result = await handler.Handle(new ClearWishlistCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.ItemCount.Should().Be(0);
        wishlist.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SetWishlistSharing_EnablesPublicShareToken()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var handler = new SetWishlistSharingCommandHandler(
            _context,
            _currentUserServiceMock.Object,
            CreateWishlistService());

        var result = await handler.Handle(
            new SetWishlistSharingCommand { IsPublic = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPublic.Should().BeTrue();
        result.Value.ShareToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegenerateShareToken_ReplacesExistingTokenAndKeepsWishlistPublic()
    {
        var userId = Guid.NewGuid();
        var wishlist = new Core.Entities.Wishlist(userId);
        wishlist.SetPublic(true);
        var originalToken = wishlist.ShareToken;
        _context.AddWishlist(wishlist);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var handler = new RegenerateWishlistShareTokenCommandHandler(
            _context,
            _currentUserServiceMock.Object,
            CreateWishlistService());

        var result = await handler.Handle(
            new RegenerateWishlistShareTokenCommand(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPublic.Should().BeTrue();
        result.Value.ShareToken.Should().NotBeNullOrWhiteSpace();
        result.Value.ShareToken.Should().NotBe(originalToken);
    }

    [Fact]
    public async Task GetWishlist_ReturnsServerItemsForAuthenticatedUser()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct("WISH-007", "Server Item AC", "server-item-ac");
        var wishlist = new Core.Entities.Wishlist(userId);
        wishlist.AddItem(product.Id);
        _context.AddProduct(product);
        _context.AddWishlist(wishlist);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var handler = new GetWishlistQueryHandler(
            _currentUserServiceMock.Object,
            CreateWishlistService());

        var result = await handler.Handle(new GetWishlistQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        result.Items[0].ProductSlug.Should().Be("server-item-ac");
    }

    [Fact]
    public async Task GetSharedWishlist_ReturnsOnlyPublicWishlist()
    {
        var product = CreateProduct("WISH-008", "Shared Item AC", "shared-item-ac");
        var wishlist = new Core.Entities.Wishlist(Guid.NewGuid());
        wishlist.AddItem(product.Id);
        wishlist.SetPublic(true);
        var token = wishlist.ShareToken!;
        _context.AddProduct(product);
        _context.AddWishlist(wishlist);

        var handler = new GetSharedWishlistQueryHandler(CreateWishlistService());

        var result = await handler.Handle(
            new GetSharedWishlistQuery { ShareToken = token },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsPublic.Should().BeTrue();
        result.Items.Should().ContainSingle(i => i.ProductSlug == "shared-item-ac");

        wishlist.SetPublic(false);
        var privateResult = await handler.Handle(
            new GetSharedWishlistQuery { ShareToken = token },
            CancellationToken.None);

        privateResult.Should().BeNull();
    }

    [Fact]
    public async Task MutationsReturnFailure_WhenUserIsUnauthenticated()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var handler = new ClearWishlistCommandHandler(
            _context,
            _currentUserServiceMock.Object,
            CreateWishlistService());

        var result = await handler.Handle(new ClearWishlistCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User must be authenticated");
    }

    private WishlistApplicationService CreateWishlistService() => new(_context);

    private static Product CreateProduct(string sku, string name, string slug)
    {
        var product = new Product(sku, name, slug, 1299.99m);
        product.SetShortDescription($"{name} short description");
        product.SetBrand("CDL");
        product.SetCompareAtPrice(1499.99m);

        var image = new ProductImage(product.Id, $"/images/{slug}.webp");
        image.SetPrimary(true);
        product.Images.Add(image);

        var variant = new ProductVariant(product.Id, $"{sku}-STD", "Standard");
        variant.SetStockQuantity(8);
        product.Variants.Add(variant);

        return product;
    }

    /// <summary>
    /// Mimics the shape of Npgsql's PostgresException by exposing a <c>SqlState</c> property,
    /// which <see cref="WishlistApplicationService.IsUniqueViolation"/> inspects via reflection
    /// (the Application layer has no Npgsql dependency).
    /// </summary>
    private sealed class FakePostgresException : Exception
    {
        public FakePostgresException(string sqlState)
            : base("Simulated Postgres error")
        {
            SqlState = sqlState;
        }

        public string SqlState { get; }
    }
}
