using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Gdpr.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Moq;

namespace ClimaSite.Application.Tests.Features.Gdpr.Queries;

public class ExportUserDataQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly MockDbContext _context = new();

    private ExportUserDataQueryHandler CreateHandler() =>
        new(_context, _currentUserServiceMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new ExportUserDataQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not authenticated");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Authenticated id with no matching row -> "User not found".
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(new ExportUserDataQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_ReturnsProfile_ForExistingUser()
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(CreateUser(userId));
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new ExportUserDataQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Profile.Id.Should().Be(userId);
        result.Value.Profile.Email.Should().Be("export@test.com");
        result.Value.Profile.FirstName.Should().Be("Ex");
        result.Value.Profile.LastName.Should().Be("Porter");
        result.Value.ExportVersion.Should().Be("1.0");
        result.Value.ExportedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_WithNoRelatedData_ReturnsEmptyCollections()
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(CreateUser(userId));
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new ExportUserDataQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Addresses.Should().BeEmpty();
        result.Value.Orders.Should().BeEmpty();
        result.Value.Reviews.Should().BeEmpty();
        result.Value.WishlistItems.Should().BeEmpty();
        result.Value.Questions.Should().BeEmpty();
        result.Value.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsUsersAddresses_AndExcludesOtherUsers()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _context.Users.Add(CreateUser(userId));

        var mine = new Address(userId, "Ex Porter", "1 Cool St", "Sofia", "1000", "Bulgaria", "BG");
        mine.SetDefault(true);
        var theirs = new Address(otherUserId, "Other Person", "9 Hot Rd", "Berlin", "10115", "Germany", "DE");
        _context.Addresses.Add(mine);
        _context.Addresses.Add(theirs);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new ExportUserDataQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Addresses.Should().ContainSingle();
        var dto = result.Value.Addresses[0];
        dto.FullName.Should().Be("Ex Porter");
        dto.City.Should().Be("Sofia");
        dto.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsOrdersWithItems_ForUser()
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(CreateUser(userId));

        var product = CreateProduct("EXP-001", "Export AC", "export-ac");
        _context.AddProduct(product);

        var order = new Order("ORD-EXP-001", "export@test.com");
        order.SetUser(userId);
        order.SetPaymentMethod("card");
        var variant = product.Variants.First();
        order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, 2, 100m);
        _context.AddOrder(order);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new ExportUserDataQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Orders.Should().ContainSingle();
        var exportedOrder = result.Value.Orders[0];
        exportedOrder.OrderNumber.Should().Be("ORD-EXP-001");
        exportedOrder.PaymentMethod.Should().Be("card");
        exportedOrder.Items.Should().ContainSingle();
        exportedOrder.Items[0].ProductName.Should().Be("Export AC");
        exportedOrder.Items[0].Quantity.Should().Be(2);
        exportedOrder.Items[0].LineTotal.Should().Be(200m); // 2 * 100
    }

    [Fact]
    public async Task Handle_ReturnsReviews_WithProductName()
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(CreateUser(userId));

        var product = CreateProduct("EXP-002", "Reviewed AC", "reviewed-ac");
        _context.AddProduct(product);

        var review = new Review(product.Id, userId, 4);
        review.SetTitle("Solid");
        review.SetContent("Quiet and efficient.");
        SetNavigation(review, nameof(Review.Product), product);
        _context.Reviews.Add(review);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new ExportUserDataQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Reviews.Should().ContainSingle();
        var dto = result.Value.Reviews[0];
        dto.ProductName.Should().Be("Reviewed AC");
        dto.Rating.Should().Be(4);
        dto.Title.Should().Be("Solid");
    }

    [Fact]
    public async Task Handle_ReturnsWishlistItems_ForUser()
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(CreateUser(userId));

        var product = CreateProduct("EXP-003", "Wished AC", "wished-ac");
        _context.AddProduct(product);

        var wishlist = new Core.Entities.Wishlist(userId);
        var item = wishlist.AddItem(product.Id);
        // The export joins WishlistItem -> Wishlist (UserId) and -> Product (Name).
        SetNavigation(item, nameof(WishlistItem.Wishlist), wishlist);
        SetNavigation(item, nameof(WishlistItem.Product), product);
        _context.AddWishlist(wishlist);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new ExportUserDataQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WishlistItems.Should().ContainSingle();
        result.Value.WishlistItems[0].ProductId.Should().Be(product.Id);
        result.Value.WishlistItems[0].ProductName.Should().Be("Wished AC");
    }

    [Fact]
    public async Task Handle_ReturnsQuestionsWithAnswers_ForUser()
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(CreateUser(userId));

        var product = CreateProduct("EXP-004", "Asked AC", "asked-ac");
        _context.AddProduct(product);

        var question = new ProductQuestion(product.Id, "Is it quiet?");
        question.SetUser(userId);
        SetNavigation(question, nameof(ProductQuestion.Product), product);

        var answer = new ProductAnswer(question.Id, "Yes, very quiet.");
        answer.SetUser(userId);
        question.Answers.Add(answer);

        _context.ProductQuestions.Add(question);
        _context.ProductAnswers.Add(answer);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new ExportUserDataQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Questions.Should().ContainSingle();
        var q = result.Value.Questions[0];
        q.QuestionText.Should().Be("Is it quiet?");
        q.ProductName.Should().Be("Asked AC");
        q.Answers.Should().ContainSingle();
        q.Answers[0].AnswerText.Should().Be("Yes, very quiet.");
    }

    [Fact]
    public async Task Handle_ReturnsNotifications_AndExcludesOtherUsers()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _context.Users.Add(CreateUser(userId));

        _context.Notifications.Add(
            new Notification(userId, NotificationTypes.OrderPlaced, "Order placed", "Your order is in."));
        _context.Notifications.Add(
            new Notification(otherUserId, NotificationTypes.OrderShipped, "Shipped", "On its way."));

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new ExportUserDataQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Notifications.Should().ContainSingle();
        result.Value.Notifications[0].Title.Should().Be("Order placed");
        result.Value.Notifications[0].Type.Should().Be(NotificationTypes.OrderPlaced);
    }

    private static ApplicationUser CreateUser(Guid userId) => new()
    {
        Id = userId,
        Email = "export@test.com",
        UserName = "export@test.com",
        FirstName = "Ex",
        LastName = "Porter",
        IsActive = true,
        PreferredLanguage = "en",
        PreferredCurrency = "EUR",
        CreatedAt = DateTime.UtcNow
    };

    private static Product CreateProduct(string sku, string name, string slug)
    {
        var product = new Product(sku, name, slug, 499.99m);
        var variant = new ProductVariant(product.Id, $"{sku}-STD", "Standard");
        variant.SetStockQuantity(10);
        product.Variants.Add(variant);
        return product;
    }

    /// <summary>
    /// Wires an EF navigation property that has a private setter. The export query projects across
    /// navigations (e.g. <c>review.Product.Name</c>); <see cref="MockDbContext"/>'s LINQ-to-objects
    /// provider does not run <c>Include</c>, so the navigation must be set explicitly in the test.
    /// </summary>
    private static void SetNavigation(object entity, string propertyName, object value)
    {
        var property = entity.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property {propertyName} not found on {entity.GetType().Name}");
        property.SetValue(entity, value);
    }
}
