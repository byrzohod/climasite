using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
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
    private readonly List<Promotion> _promotions = [];
    private readonly List<PromotionProduct> _promotionProducts = [];
    private readonly List<PromotionTranslation> _promotionTranslations = [];
    private readonly List<Brand> _brands = [];
    private readonly List<BrandTranslation> _brandTranslations = [];
    private readonly List<CategoryTranslation> _categoryTranslations = [];
    private readonly List<ProductQuestion> _productQuestions = [];
    private readonly List<ProductAnswer> _productAnswers = [];
    private readonly List<ProductQuestionVote> _productQuestionVotes = [];
    private readonly List<ProductAnswerVote> _productAnswerVotes = [];
    private readonly List<InstallationRequest> _installationRequests = [];
    private readonly List<ProductPriceHistory> _productPriceHistory = [];
    private readonly List<ReviewVote> _reviewVotes = [];
    private readonly List<OutboxMessage> _outboxMessages = [];
    private readonly List<ContactMessage> _contactMessages = [];

    public DatabaseFacade Database { get; }

    /// <summary>
    /// How many times the execution-strategy delegate is invoked. Defaults to 1 (pass-through). Tests set
    /// this to 2 to SIMULATE a commit-unknown retry (EnableRetryOnFailure) and assert the net effect is
    /// applied exactly once — the critical idempotency case for the toggle/flip vote paths.
    /// </summary>
    public int ExecutionStrategyAttempts { get; set; } = 1;

    public MockDbContext()
    {
        Database = CreateMockDatabaseFacade(() => ExecutionStrategyAttempts);
    }

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
    public DbSet<ReviewVote> ReviewVotes => CreateMockDbSet(_reviewVotes);
    public DbSet<Wishlist> Wishlists => CreateMockDbSet(_wishlists);
    public DbSet<WishlistItem> WishlistItems => CreateMockDbSet(_wishlistItems);
    public DbSet<Address> Addresses => CreateMockDbSet(_addresses);
    public DbSet<RelatedProduct> RelatedProducts => CreateMockDbSet(_relatedProducts);
    public DbSet<ApplicationUser> Users => CreateMockDbSet(_users);
    public DbSet<Notification> Notifications => CreateMockDbSet(_notifications);
    public DbSet<ProductTranslation> ProductTranslations => CreateMockDbSet(_productTranslations);
    public DbSet<Promotion> Promotions => CreateMockDbSet(_promotions);
    public DbSet<PromotionProduct> PromotionProducts => CreateMockDbSet(_promotionProducts);
    public DbSet<PromotionTranslation> PromotionTranslations => CreateMockDbSet(_promotionTranslations);
    public DbSet<Brand> Brands => CreateMockDbSet(_brands);
    public DbSet<BrandTranslation> BrandTranslations => CreateMockDbSet(_brandTranslations);
    public DbSet<CategoryTranslation> CategoryTranslations => CreateMockDbSet(_categoryTranslations);
    public DbSet<ProductQuestion> ProductQuestions => CreateMockDbSet(_productQuestions);
    public DbSet<ProductAnswer> ProductAnswers => CreateMockDbSet(_productAnswers);
    public DbSet<ProductQuestionVote> ProductQuestionVotes => CreateMockDbSet(_productQuestionVotes);
    public DbSet<ProductAnswerVote> ProductAnswerVotes => CreateMockDbSet(_productAnswerVotes);
    public DbSet<InstallationRequest> InstallationRequests => CreateMockDbSet(_installationRequests);
    public DbSet<ProductPriceHistory> ProductPriceHistory => CreateMockDbSet(_productPriceHistory);
    public DbSet<OutboxMessage> OutboxMessages => CreateMockDbSet(_outboxMessages);
    public DbSet<ContactMessage> ContactMessages => CreateMockDbSet(_contactMessages);

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

    public void AddWishlist(Wishlist wishlist)
    {
        _wishlists.Add(wishlist);
        foreach (var item in wishlist.Items)
        {
            _wishlistItems.Add(item);
        }
    }

    public void AddOutboxMessage(OutboxMessage message) => _outboxMessages.Add(message);

    public void AddInstallationRequest(InstallationRequest request) => _installationRequests.Add(request);

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

        foreach (var wishlist in _wishlists)
        {
            foreach (var item in wishlist.Items)
            {
                if (!_wishlistItems.Contains(item))
                {
                    _wishlistItems.Add(item);
                }
            }
        }

        return Task.FromResult(1);
    }

    public Task<int> TryDecrementVariantStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        var variant = _productVariants.FirstOrDefault(v => v.Id == variantId);
        if (variant is null || variant.StockQuantity < quantity)
        {
            return Task.FromResult(0);
        }

        variant.AdjustStock(-quantity);
        return Task.FromResult(1);
    }

    // ---- B-039: in-memory simulations of the atomic Q&A vote SQL primitives (mirror the real
    // ON CONFLICT / conditional DELETE / conditional UPDATE / floored count-adjust semantics). ----

    public Task<int> TryInsertQuestionVoteAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (_productQuestionVotes.Any(v => v.QuestionId == questionId && v.UserId == userId))
        {
            return Task.FromResult(0); // ON CONFLICT DO NOTHING
        }

        _productQuestionVotes.Add(new ProductQuestionVote(questionId, userId));
        return Task.FromResult(1);
    }

    public Task<int> DeleteQuestionVoteAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var removed = _productQuestionVotes.RemoveAll(v => v.QuestionId == questionId && v.UserId == userId);
        return Task.FromResult(removed);
    }

    public Task<int> AdjustQuestionHelpfulCountAsync(Guid questionId, int delta, CancellationToken cancellationToken = default)
    {
        var question = _productQuestions.FirstOrDefault(q => q.Id == questionId);
        if (question is null || (delta < 0 && question.HelpfulCount == 0))
        {
            return Task.FromResult(0);
        }

        for (var i = 0; i < Math.Abs(delta); i++)
        {
            if (delta > 0) question.AddHelpfulVote();
            else question.RemoveHelpfulVote();
        }
        return Task.FromResult(1);
    }

    public Task<int> TryInsertAnswerVoteAsync(Guid answerId, Guid userId, bool isHelpful, CancellationToken cancellationToken = default)
    {
        if (_productAnswerVotes.Any(v => v.AnswerId == answerId && v.UserId == userId))
        {
            return Task.FromResult(0); // ON CONFLICT DO NOTHING
        }

        _productAnswerVotes.Add(new ProductAnswerVote(answerId, userId, isHelpful));
        return Task.FromResult(1);
    }

    public Task<int> DeleteAnswerVoteAsync(Guid answerId, Guid userId, bool isHelpful, CancellationToken cancellationToken = default)
    {
        var removed = _productAnswerVotes.RemoveAll(
            v => v.AnswerId == answerId && v.UserId == userId && v.IsHelpful == isHelpful);
        return Task.FromResult(removed);
    }

    public Task<int> FlipAnswerVoteAsync(Guid answerId, Guid userId, bool fromHelpful, bool toHelpful, CancellationToken cancellationToken = default)
    {
        var vote = _productAnswerVotes.FirstOrDefault(
            v => v.AnswerId == answerId && v.UserId == userId && v.IsHelpful == fromHelpful);
        if (vote is null)
        {
            return Task.FromResult(0);
        }

        vote.ChangeVote(toHelpful);
        return Task.FromResult(1);
    }

    public Task<int> AdjustAnswerVoteCountAsync(Guid answerId, bool helpful, int delta, CancellationToken cancellationToken = default)
    {
        var answer = _productAnswers.FirstOrDefault(a => a.Id == answerId);
        if (answer is null)
        {
            return Task.FromResult(0);
        }

        var current = helpful ? answer.HelpfulCount : answer.UnhelpfulCount;
        if (delta < 0 && current == 0)
        {
            return Task.FromResult(0);
        }

        for (var i = 0; i < Math.Abs(delta); i++)
        {
            if (helpful)
            {
                if (delta > 0) answer.AddHelpfulVote();
                else answer.RemoveHelpfulVote();
            }
            else
            {
                if (delta > 0) answer.AddUnhelpfulVote();
                else answer.RemoveUnhelpfulVote();
            }
        }
        return Task.FromResult(1);
    }

    private static DatabaseFacade CreateMockDatabaseFacade(Func<int> attemptsProvider)
    {
        var mockTransaction = new Mock<IDbContextTransaction>();
        mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransaction.Setup(t => t.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var mockDbContext = new Mock<DbContext>();
        var mockFacade = new Mock<DatabaseFacade>(mockDbContext.Object);
        mockFacade.Setup(f => f.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);
        // Provide an execution strategy so production code can call strategy.ExecuteAsync(...) without a
        // real provider. It invokes the delegate attemptsProvider() times (default 1) so tests can
        // simulate a retry (2 invocations) and prove the vote paths are idempotent.
        mockFacade.Setup(f => f.CreateExecutionStrategy())
            .Returns(() => new ConfigurableExecutionStrategy(attemptsProvider));

        return mockFacade.Object;
    }

    private static DbSet<T> CreateMockDbSet<T>(List<T> data) where T : class
    {
        // Wrap the backing list in a TestAsyncEnumerable so that LINQ operators applied directly to
        // the DbSet (including a redundant .AsQueryable()) keep flowing through the async-capable
        // provider — handlers that do _context.X.AsQueryable()...CountAsync()/ToListAsync() rely on it.
        IQueryable<T> queryable = new TestAsyncEnumerable<T>(data);
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

        // DbSet<T> exposes a VIRTUAL instance method AsQueryable(); it shadows the
        // System.Linq.Queryable.AsQueryable extension at the call site, so handlers that do
        // _context.X.AsQueryable()...CountAsync()/ToListAsync() hit this method, not the extension.
        // Left unconfigured, Moq returns a plain non-async EnumerableQuery and EF Core's async
        // operators throw "provider ... doesn't implement IAsyncQueryProvider". Returning the
        // async-capable TestAsyncEnumerable keeps those chains working.
        mockSet.Setup(m => m.AsQueryable()).Returns(() => new TestAsyncEnumerable<T>(data));

        // FindAsync(id) — look the entity up by its Id in the backing list, the way EF Core would
        // resolve a primary-key lookup. Supports both DbSet.FindAsync overloads:
        //   FindAsync(object?[] keyValues, CancellationToken) and FindAsync(params object?[] keyValues).
        // Returns default(T) (null) when no match, so production "not found" branches stay reachable.
        mockSet.Setup(m => m.FindAsync(It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns((object?[] keyValues, CancellationToken _) =>
                new ValueTask<T?>(FindById(data, keyValues)));

        mockSet.Setup(m => m.FindAsync(It.IsAny<object?[]>()))
            .Returns((object?[] keyValues) => new ValueTask<T?>(FindById(data, keyValues)));

        mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => data.Add(entity))
            .Returns((T _, CancellationToken _) => default(ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>>));

        mockSet.Setup(m => m.Add(It.IsAny<T>()))
            .Callback<T>(entity => data.Add(entity));

        mockSet.Setup(m => m.AddRangeAsync(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<T>, CancellationToken>((entities, _) => data.AddRange(entities))
            .Returns(Task.CompletedTask);

        mockSet.Setup(m => m.Remove(It.IsAny<T>()))
            .Callback<T>(entity => data.Remove(entity));

        mockSet.Setup(m => m.RemoveRange(It.IsAny<IEnumerable<T>>()))
            .Callback<IEnumerable<T>>(entities =>
            {
                foreach (var entity in entities.ToList())
                {
                    data.Remove(entity);
                }
            });

        return mockSet.Object;
    }

    /// <summary>
    /// Resolves a single entity by its primary key from the backing list, mirroring how EF Core's
    /// <c>FindAsync</c> matches on the "Id" key. Returns <c>null</c> when no entity matches (or the
    /// key is empty), keeping production "not found" branches reachable in unit tests.
    /// </summary>
    private static T? FindById<T>(List<T> data, object?[]? keyValues) where T : class
    {
        if (keyValues is null || keyValues.Length == 0 || keyValues[0] is null)
        {
            return null;
        }

        var key = keyValues[0]!;
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty is null)
        {
            return null;
        }

        return data.FirstOrDefault(entity => Equals(idProperty.GetValue(entity), key));
    }
}

/// <summary>
/// An execution strategy that invokes the supplied operation a configurable number of times. With the
/// default of 1 it is a plain pass-through (lets unit tests exercise
/// <c>Database.CreateExecutionStrategy().ExecuteAsync(...)</c> without a real provider). With 2 it
/// simulates a commit-unknown retry (as <c>EnableRetryOnFailure</c> would do), so tests can prove the
/// vote paths are idempotent — the from-state-guarded conditional ops must apply the net effect once.
/// </summary>
internal sealed class ConfigurableExecutionStrategy : IExecutionStrategy
{
    private readonly Func<int> _attemptsProvider;

    public ConfigurableExecutionStrategy(Func<int> attemptsProvider)
    {
        _attemptsProvider = attemptsProvider;
    }

    public bool RetriesOnFailure => false;

    public TResult Execute<TState, TResult>(
        TState state,
        Func<DbContext, TState, TResult> operation,
        Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
    {
        var attempts = Math.Max(1, _attemptsProvider());
        TResult result = default!;
        for (var i = 0; i < attempts; i++)
        {
            result = operation(null!, state);
        }
        return result;
    }

    public async Task<TResult> ExecuteAsync<TState, TResult>(
        TState state,
        Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
        Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
        CancellationToken cancellationToken = default)
    {
        var attempts = Math.Max(1, _attemptsProvider());
        TResult result = default!;
        for (var i = 0; i < attempts; i++)
        {
            result = await operation(null!, state, cancellationToken);
        }
        return result;
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
