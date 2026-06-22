using ClimaSite.Application.Features.Admin.Customers.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Customers.Queries;

// Exercises the admin customer-listing pipeline: _context.Users.AsQueryable() with search/status/date
// filters and sorting, plus a CountAsync()/ToListAsync() and an order-stats GroupBy().ToDictionaryAsync()
// over _context.Orders — all paths the enhanced MockDbContext now supports.
public class GetAdminCustomersQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetAdminCustomersQueryHandler CreateHandler() => new(_context);

    private static void SetCreatedAt(ApplicationUser user, DateTime createdAt) => user.CreatedAt = createdAt;

    private ApplicationUser SeedUser(
        string email,
        string firstName,
        string lastName,
        bool isActive = true,
        bool emailConfirmed = true,
        string? phone = null,
        DateTime? createdAt = null,
        DateTime? lastLoginAt = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = isActive,
            EmailConfirmed = emailConfirmed,
            PhoneNumber = phone,
            LastLoginAt = lastLoginAt
        };
        if (createdAt.HasValue)
        {
            SetCreatedAt(user, createdAt.Value);
        }
        _context.Users.Add(user);
        return user;
    }

    private static Product CreateProduct()
    {
        var product = new Product("CUST-PROD", "Customer Product", "customer-product", 100m);
        var variant = new ProductVariant(product.Id, "CUST-PROD-V", "Default");
        variant.SetStockQuantity(50);
        product.Variants.Add(variant);
        return product;
    }

    private void SeedOrderForUser(Product product, Guid userId, decimal unitPrice, OrderStatus? status = null)
    {
        var order = new Order($"ORD-{Guid.NewGuid():N}", "buyer@test.com");
        order.SetUser(userId);
        var variant = product.Variants.First();
        order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, 1, unitPrice);
        WalkToStatus(order, status);
        _context.AddOrder(order);
    }

    // Order status transitions are domain-validated (Pending -> Paid -> Processing -> Shipped -> ...),
    // so reaching a later status requires walking the full valid path rather than a single jump.
    private static void WalkToStatus(Order order, OrderStatus? target)
    {
        if (!target.HasValue || target.Value == OrderStatus.Pending)
        {
            return;
        }

        switch (target.Value)
        {
            case OrderStatus.Cancelled:
                order.SetStatus(OrderStatus.Cancelled);
                break;
            case OrderStatus.Paid:
                order.SetStatus(OrderStatus.Paid);
                break;
            case OrderStatus.Processing:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Processing);
                break;
            case OrderStatus.Shipped:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Processing);
                order.SetStatus(OrderStatus.Shipped);
                break;
            case OrderStatus.Delivered:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Processing);
                order.SetStatus(OrderStatus.Shipped);
                order.SetStatus(OrderStatus.Delivered);
                break;
            default:
                order.SetStatus(OrderStatus.Paid);
                break;
        }
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoUsers()
    {
        var result = await CreateHandler().Handle(new GetAdminCustomersQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MapsUserFieldsToListItem()
    {
        var user = SeedUser("jane@test.com", "Jane", "Buyer", phone: "+359888123456");

        var result = await CreateHandler().Handle(new GetAdminCustomersQuery(), CancellationToken.None);

        var item = result.Items.Should().ContainSingle().Subject;
        item.Id.Should().Be(user.Id);
        item.Email.Should().Be("jane@test.com");
        item.FullName.Should().Be("Jane Buyer");
        item.Phone.Should().Be("+359888123456");
        item.IsActive.Should().BeTrue();
        item.EmailConfirmed.Should().BeTrue();
        item.OrderCount.Should().Be(0);
        item.TotalSpent.Should().Be(0);
    }

    [Fact]
    public async Task Handle_AggregatesOrderStats_ExcludingCancelledOrders()
    {
        var product = CreateProduct();
        _context.AddProduct(product);
        var user = SeedUser("spender@test.com", "Big", "Spender");

        SeedOrderForUser(product, user.Id, 100m, OrderStatus.Paid);
        SeedOrderForUser(product, user.Id, 250m, OrderStatus.Delivered);
        SeedOrderForUser(product, user.Id, 999m, OrderStatus.Cancelled); // excluded from stats

        var result = await CreateHandler().Handle(new GetAdminCustomersQuery(), CancellationToken.None);

        var item = result.Items.Should().ContainSingle().Subject;
        item.OrderCount.Should().Be(2, "cancelled orders are excluded");
        item.TotalSpent.Should().Be(350m);
    }

    [Fact]
    public async Task Handle_SearchMatchesEmailNameOrPhone_CaseInsensitively()
    {
        SeedUser("alice@example.com", "Alice", "Anderson", phone: "111");
        SeedUser("bob@other.com", "Bob", "Brown", phone: "222");

        var result = await CreateHandler().Handle(
            new GetAdminCustomersQuery { Search = "ANDERSON" }, CancellationToken.None);

        result.Items.Should().ContainSingle(c => c.Email == "alice@example.com");
    }

    [Fact]
    public async Task Handle_StatusFilter_Active_ReturnsOnlyActiveUsers()
    {
        SeedUser("active@test.com", "Act", "Ive", isActive: true);
        SeedUser("inactive@test.com", "In", "Active", isActive: false);

        var result = await CreateHandler().Handle(
            new GetAdminCustomersQuery { Status = "inactive" }, CancellationToken.None);

        result.Items.Should().ContainSingle(c => c.Email == "inactive@test.com");
    }

    [Fact]
    public async Task Handle_StatusFilter_Unverified_ReturnsUnconfirmedUsers()
    {
        SeedUser("confirmed@test.com", "Con", "Firmed", emailConfirmed: true);
        SeedUser("pending@test.com", "Pen", "Ding", emailConfirmed: false);

        var result = await CreateHandler().Handle(
            new GetAdminCustomersQuery { Status = "unverified" }, CancellationToken.None);

        result.Items.Should().ContainSingle(c => c.Email == "pending@test.com");
    }

    [Fact]
    public async Task Handle_RegistrationDateRange_FiltersUsers()
    {
        SeedUser("old@test.com", "Old", "User", createdAt: DateTime.UtcNow.AddDays(-10));
        SeedUser("new@test.com", "New", "User", createdAt: DateTime.UtcNow.AddDays(-1));

        var result = await CreateHandler().Handle(
            new GetAdminCustomersQuery { RegisteredFrom = DateTime.UtcNow.AddDays(-3) }, CancellationToken.None);

        result.Items.Should().ContainSingle(c => c.Email == "new@test.com");
    }

    [Fact]
    public async Task Handle_SortsByEmailAscending()
    {
        SeedUser("charlie@test.com", "Charlie", "C");
        SeedUser("alice@test.com", "Alice", "A");
        SeedUser("bob@test.com", "Bob", "B");

        var result = await CreateHandler().Handle(
            new GetAdminCustomersQuery { SortBy = "email", SortOrder = "asc" }, CancellationToken.None);

        result.Items.Select(c => c.Email)
            .Should().ContainInOrder("alice@test.com", "bob@test.com", "charlie@test.com");
    }

    [Fact]
    public async Task Handle_DefaultsToCreatedAtDescending()
    {
        SeedUser("first@test.com", "First", "User", createdAt: DateTime.UtcNow.AddDays(-3));
        SeedUser("second@test.com", "Second", "User", createdAt: DateTime.UtcNow.AddDays(-1));

        var result = await CreateHandler().Handle(new GetAdminCustomersQuery(), CancellationToken.None);

        result.Items.Select(c => c.Email).Should().ContainInOrder("second@test.com", "first@test.com");
    }

    [Fact]
    public async Task Handle_AppliesPagingAndComputesTotalPages()
    {
        for (var i = 0; i < 5; i++)
        {
            SeedUser($"user{i}@test.com", $"User{i}", "Test", createdAt: DateTime.UtcNow.AddMinutes(-i));
        }

        var result = await CreateHandler().Handle(
            new GetAdminCustomersQuery { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(2);
    }
}
