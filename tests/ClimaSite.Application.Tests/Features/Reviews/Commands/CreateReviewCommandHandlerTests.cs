using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Reviews.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Reviews.Commands;

public class CreateReviewCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private CreateReviewCommandHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    [Fact]
    public async Task Handle_CreatesReview_WithApprovedStatus_SoItAppearsImmediately()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct("REV-001", "Reviewable AC", "reviewable-ac");
        _context.AddProduct(product);
        _context.Users.Add(CreateUser(userId));
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new CreateReviewCommand
            {
                ProductId = product.Id,
                Rating = 5,
                Title = "Excellent",
                Content = "Cools the room in minutes."
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // The owner's decision: reviews appear immediately (auto-approved on submit).
        result.Value!.Status.Should().Be(ReviewStatus.Approved.ToString());

        var stored = _context.Reviews.Single();
        stored.Status.Should().Be(ReviewStatus.Approved);
    }

    [Fact]
    public async Task Handle_SetsVerifiedPurchase_WhenUserHasDeliveredOrderForProduct()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct("REV-002", "Verified AC", "verified-ac");
        _context.AddProduct(product);
        _context.Users.Add(CreateUser(userId));
        _context.AddOrder(CreateDeliveredOrder(userId, product));
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new CreateReviewCommand { ProductId = product.Id, Rating = 4 },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Auto-approval must not clobber the verified-purchase badge.
        result.Value!.IsVerifiedPurchase.Should().BeTrue();
        result.Value.Status.Should().Be(ReviewStatus.Approved.ToString());
        _context.Reviews.Single().IsVerifiedPurchase.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RejectsDuplicateReview_FromSameUser()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct("REV-003", "Once AC", "once-ac");
        _context.AddProduct(product);
        _context.Users.Add(CreateUser(userId));
        _context.Reviews.Add(new Review(product.Id, userId, 3));
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(
            new CreateReviewCommand { ProductId = product.Id, Rating = 5 },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already reviewed");
        _context.Reviews.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Fails_WhenUserNotAuthenticated()
    {
        var product = CreateProduct("REV-004", "Anon AC", "anon-ac");
        _context.AddProduct(product);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new CreateReviewCommand { ProductId = product.Id, Rating = 5 },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _context.Reviews.Should().BeEmpty();
    }

    private static Product CreateProduct(string sku, string name, string slug)
    {
        var product = new Product(sku, name, slug, 999.99m);
        var variant = new ProductVariant(product.Id, $"{sku}-STD", "Standard");
        variant.SetStockQuantity(5);
        product.Variants.Add(variant);
        return product;
    }

    private static ApplicationUser CreateUser(Guid userId) => new()
    {
        Id = userId,
        Email = "buyer@example.com",
        UserName = "buyer@example.com",
        FirstName = "Jane",
        LastName = "Buyer"
    };

    private static Order CreateDeliveredOrder(Guid userId, Product product)
    {
        var order = new Order($"ORD-{Guid.NewGuid():N}".Substring(0, 12), "buyer@example.com");
        order.SetUser(userId);
        var variant = product.Variants.First();
        order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, 1, 999.99m);

        // Walk the order through valid transitions to reach Delivered.
        order.SetStatus(OrderStatus.Paid);
        order.SetStatus(OrderStatus.Processing);
        order.SetStatus(OrderStatus.Shipped);
        order.SetStatus(OrderStatus.Delivered);
        return order;
    }
}
