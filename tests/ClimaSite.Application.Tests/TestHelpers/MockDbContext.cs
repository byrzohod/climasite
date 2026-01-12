using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace ClimaSite.Application.Tests.TestHelpers;

/// <summary>
/// A mock implementation of IApplicationDbContext for testing.
/// Uses in-memory lists that can be queried with LINQ.
/// </summary>
public class MockDbContext : IApplicationDbContext
{
    private readonly List<Product> _products = [];
    private readonly List<ProductVariant> _productVariants = [];
    private readonly List<ProductImage> _productImages = [];
    private readonly List<Category> _categories = [];
    private readonly List<Cart> _carts = [];
    private readonly List<CartItem> _cartItems = [];
    private readonly List<Order> _orders = [];
    private readonly List<OrderItem> _orderItems = [];
    private readonly List<OrderEvent> _orderEvents = [];
    private readonly List<Review> _reviews = [];
    private readonly List<Wishlist> _wishlists = [];
    private readonly List<WishlistItem> _wishlistItems = [];
    private readonly List<Address> _addresses = [];
    private readonly List<RelatedProduct> _relatedProducts = [];
    private readonly List<ApplicationUser> _users = [];
    private readonly List<Notification> _notifications = [];
    private readonly List<ProductTranslation> _productTranslations = [];

    public DbSet<Product> Products => CreateMockDbSet(_products);
    public DbSet<ProductVariant> ProductVariants => CreateMockDbSet(_productVariants);
    public DbSet<ProductImage> ProductImages => CreateMockDbSet(_productImages);
    public DbSet<Category> Categories => CreateMockDbSet(_categories);
    public DbSet<Cart> Carts => CreateMockDbSet(_carts);
    public DbSet<CartItem> CartItems => CreateMockDbSet(_cartItems);
    public DbSet<Order> Orders => CreateMockDbSet(_orders);
    public DbSet<OrderItem> OrderItems => CreateMockDbSet(_orderItems);
    public DbSet<OrderEvent> OrderEvents => CreateMockDbSet(_orderEvents);
    public DbSet<Review> Reviews => CreateMockDbSet(_reviews);
    public DbSet<Wishlist> Wishlists => CreateMockDbSet(_wishlists);
    public DbSet<WishlistItem> WishlistItems => CreateMockDbSet(_wishlistItems);
    public DbSet<Address> Addresses => CreateMockDbSet(_addresses);
    public DbSet<RelatedProduct> RelatedProducts => CreateMockDbSet(_relatedProducts);
    public DbSet<ApplicationUser> Users => CreateMockDbSet(_users);
    public DbSet<Notification> Notifications => CreateMockDbSet(_notifications);
    public DbSet<ProductTranslation> ProductTranslations => CreateMockDbSet(_productTranslations);

    public void AddProduct(Product product)
    {
        _products.Add(product);
        foreach (var variant in product.Variants)
        {
            _productVariants.Add(variant);
        }
        foreach (var image in product.Images)
        {
            _productImages.Add(image);
        }
    }

    public void AddOrder(Order order)
    {
        _orders.Add(order);
        foreach (var item in order.Items)
        {
            _orderItems.Add(item);

            // Link the Product navigation property if the product exists
            var product = _products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null)
            {
                // Use reflection to set the navigation property since it may be protected/private
                var productProperty = typeof(OrderItem).GetProperty("Product");
                productProperty?.SetValue(item, product);
            }
        }
        foreach (var evt in order.Events)
        {
            _orderEvents.Add(evt);
        }
    }

    public void AddCart(Cart cart)
    {
        _carts.Add(cart);
        foreach (var item in cart.Items)
        {
            _cartItems.Add(item);
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Sync any new items that might have been added to navigation properties
        foreach (var order in _orders)
        {
            foreach (var item in order.Items)
            {
                if (!_orderItems.Contains(item))
                {
                    _orderItems.Add(item);
                }
            }
            foreach (var evt in order.Events)
            {
                if (!_orderEvents.Contains(evt))
                {
                    _orderEvents.Add(evt);
                }
            }
        }

        return Task.FromResult(1);
    }

    private static DbSet<T> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => data.Add(entity))
            .Returns((T entity, CancellationToken _) => ValueTask.FromResult(new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>(null!)));

        mockSet.Setup(m => m.Add(It.IsAny<T>()))
            .Callback<T>(entity => data.Add(entity));

        mockSet.Setup(m => m.AddRangeAsync(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<T>, CancellationToken>((entities, _) => data.AddRange(entities))
            .Returns(Task.CompletedTask);

        mockSet.Setup(m => m.Remove(It.IsAny<T>()))
            .Callback<T>(entity => data.Remove(entity));

        return mockSet.Object;
    }
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(System.Linq.Expressions.Expression) })!
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public T Current => _inner.Current;
}
