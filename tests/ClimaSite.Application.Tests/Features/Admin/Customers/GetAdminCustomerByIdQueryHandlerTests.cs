using ClimaSite.Application.Features.Admin.Customers.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Customers;

public class GetAdminCustomerByIdQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetAdminCustomerByIdQueryHandler CreateHandler() => new(_context);

    private ApplicationUser SeedUser()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "detail@test.com",
            UserName = "detail@test.com",
            FirstName = "Detail",
            LastName = "Customer",
            PhoneNumber = "+359888000111",
            IsActive = true,
            EmailConfirmed = true,
            PreferredLanguage = "de",
            PreferredCurrency = "EUR"
        };
        _context.Users.Add(user);
        return user;
    }

    private static Product CreateProduct()
    {
        var product = new Product("DET-PROD", "Detail Product", "detail-product", 100m);
        var variant = new ProductVariant(product.Id, "DET-PROD-V", "Default");
        variant.SetStockQuantity(20);
        product.Variants.Add(variant);
        return product;
    }

    private Order SeedOrderForUser(Guid userId, Product product, decimal unitPrice, OrderStatus? status = null)
    {
        var order = new Order($"ORD-{Guid.NewGuid():N}"[..12], "buyer@test.com");
        order.SetUser(userId);
        var variant = product.Variants.First();
        order.AddItem(product.Id, variant.Id, product.Name, variant.Name, variant.Sku, 1, unitPrice);
        if (status.HasValue && status.Value != OrderStatus.Pending)
        {
            order.SetStatus(status.Value);
        }
        _context.AddOrder(order);
        return order;
    }

    private static void SetWishlistNav(ClimaSite.Core.Entities.WishlistItem item, ClimaSite.Core.Entities.Wishlist wishlist)
    {
        // The WishlistItems query filters on wi.Wishlist.UserId; the mock context does not wire the
        // navigation property, so set it here (mirrors what EF would materialise via Include).
        typeof(ClimaSite.Core.Entities.WishlistItem).GetProperty("Wishlist")!.SetValue(item, wishlist);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenCustomerNotFound()
    {
        var result = await CreateHandler().Handle(
            new GetAdminCustomerByIdQuery { Id = Guid.NewGuid() },
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MapsCustomerProfileFields()
    {
        var user = SeedUser();

        var result = await CreateHandler().Handle(
            new GetAdminCustomerByIdQuery { Id = user.Id },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("detail@test.com");
        result.FirstName.Should().Be("Detail");
        result.LastName.Should().Be("Customer");
        result.PhoneNumber.Should().Be("+359888000111");
        result.PreferredLanguage.Should().Be("de");
        result.PreferredCurrency.Should().Be("EUR");
        result.IsActive.Should().BeTrue();
        result.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_IncludesAddresses()
    {
        var user = SeedUser();
        var address = new Address(user.Id, "Detail Customer", "Main St 1", "Sofia", "1000", "Bulgaria", "BG");
        address.SetState("Sofia-Grad");
        address.SetDefault(true);
        user.Addresses.Add(address);

        var result = await CreateHandler().Handle(
            new GetAdminCustomerByIdQuery { Id = user.Id },
            CancellationToken.None);

        result!.Addresses.Should().ContainSingle();
        var dto = result.Addresses.Single();
        dto.AddressLine1.Should().Be("Main St 1");
        dto.City.Should().Be("Sofia");
        dto.State.Should().Be("Sofia-Grad");
        dto.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ComputesOrderStats_ExcludingCancelled()
    {
        var product = CreateProduct();
        _context.AddProduct(product);
        var user = SeedUser();

        SeedOrderForUser(user.Id, product, 100m);
        SeedOrderForUser(user.Id, product, 300m);
        SeedOrderForUser(user.Id, product, 500m, OrderStatus.Cancelled); // excluded from spend/avg

        var result = await CreateHandler().Handle(
            new GetAdminCustomerByIdQuery { Id = user.Id },
            CancellationToken.None);

        result!.Stats.TotalOrders.Should().Be(2);
        result.Stats.TotalSpent.Should().Be(400m);
        result.Stats.AverageOrderValue.Should().Be(200m);
        // RecentOrders still shows all orders (incl. cancelled).
        result.RecentOrders.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ReturnsZeroStats_WhenCustomerHasNoOrders()
    {
        var user = SeedUser();

        var result = await CreateHandler().Handle(
            new GetAdminCustomerByIdQuery { Id = user.Id },
            CancellationToken.None);

        result!.Stats.TotalOrders.Should().Be(0);
        result.Stats.TotalSpent.Should().Be(0);
        result.Stats.AverageOrderValue.Should().Be(0);
        result.RecentOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CountsReviewsAndWishlistItems()
    {
        var product = CreateProduct();
        _context.AddProduct(product);
        var user = SeedUser();

        _context.Reviews.Add(new Review(product.Id, user.Id, 5));
        _context.Reviews.Add(new Review(product.Id, user.Id, 4));

        var wishlist = new ClimaSite.Core.Entities.Wishlist(user.Id);
        var item = wishlist.AddItem(product.Id);
        SetWishlistNav(item, wishlist);
        _context.AddWishlist(wishlist);

        var result = await CreateHandler().Handle(
            new GetAdminCustomerByIdQuery { Id = user.Id },
            CancellationToken.None);

        result!.Stats.ReviewsWritten.Should().Be(2);
        result.Stats.WishlistItems.Should().Be(1);
    }
}
