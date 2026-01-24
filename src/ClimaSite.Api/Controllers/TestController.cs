#if DEBUG
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Api.Controllers;

/// <summary>
/// Test-only endpoints for E2E testing.
/// These endpoints are only available in Development environment.
/// This controller is only compiled in DEBUG builds.
/// </summary>
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public TestController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _configuration = configuration;
    }

    /// <summary>
    /// Elevates a user to Admin role. Only works in Development environment.
    /// </summary>
    [HttpPost("elevate-admin")]
    public async Task<IActionResult> ElevateToAdmin([FromBody] ElevateAdminRequest request)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var expectedSecret = _configuration["TestSettings:AdminSecret"] ?? "test-admin-secret";
        if (request.TestSecret != expectedSecret)
        {
            return Unauthorized(new { message = "Invalid test secret" });
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Check if user is already admin
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return Ok(new { message = "User is already an Admin" });
        }

        // Remove Customer role if present
        if (await _userManager.IsInRoleAsync(user, "Customer"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Customer");
        }

        // Add Admin role
        var result = await _userManager.AddToRoleAsync(user, "Admin");
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Failed to add Admin role", errors = result.Errors });
        }

        return Ok(new { message = "User elevated to Admin successfully" });
    }

    /// <summary>
    /// Cleans up test data by correlation ID. Only works in Development environment.
    /// Test data is identified by:
    /// - Users with emails containing the correlation ID
    /// - Products with SKUs starting with "TEST-" and containing the correlation ID
    /// </summary>
    [HttpDelete("cleanup/{correlationId}")]
    public async Task<IActionResult> CleanupTestData(string correlationId)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var cleanupResults = new CleanupResult();

        try
        {
            // Clean up orders first (due to FK constraints)
            var testOrders = await _context.Orders
                .Where(o => o.CustomerEmail.Contains(correlationId) ||
                           (o.User != null && o.User.Email != null && o.User.Email.Contains(correlationId)))
                .ToListAsync();

            foreach (var order in testOrders)
            {
                var orderItems = await _context.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync();
                _context.OrderItems.RemoveRange(orderItems);
            }
            _context.Orders.RemoveRange(testOrders);
            cleanupResults.OrdersDeleted = testOrders.Count;

            // Clean up cart items and carts
            var testCarts = await _context.Carts
                .Include(c => c.Items)
                .Where(c => c.User != null && c.User.Email != null && c.User.Email.Contains(correlationId))
                .ToListAsync();

            foreach (var cart in testCarts)
            {
                _context.CartItems.RemoveRange(cart.Items);
            }
            _context.Carts.RemoveRange(testCarts);
            cleanupResults.CartsDeleted = testCarts.Count;

            // Clean up reviews
            var testReviews = await _context.Reviews
                .Where(r => r.User != null && r.User.Email != null && r.User.Email.Contains(correlationId))
                .ToListAsync();
            _context.Reviews.RemoveRange(testReviews);
            cleanupResults.ReviewsDeleted = testReviews.Count;

            // Clean up wishlist items and wishlists
            var testWishlists = await _context.Wishlists
                .Include(w => w.Items)
                .Where(w => w.User != null && w.User.Email != null && w.User.Email.Contains(correlationId))
                .ToListAsync();

            foreach (var wishlist in testWishlists)
            {
                _context.WishlistItems.RemoveRange(wishlist.Items);
            }
            _context.Wishlists.RemoveRange(testWishlists);
            cleanupResults.WishlistsDeleted = testWishlists.Count;

            // Clean up notifications
            var testNotifications = await _context.Notifications
                .Where(n => n.User != null && n.User.Email != null && n.User.Email.Contains(correlationId))
                .ToListAsync();
            _context.Notifications.RemoveRange(testNotifications);
            cleanupResults.NotificationsDeleted = testNotifications.Count;

            // Save changes for foreign key constrained items first
            await _context.SaveChangesAsync();

            // Clean up products (by SKU prefix containing correlation ID)
            var testProducts = await _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Where(p => p.Sku.Contains(correlationId.Substring(0, Math.Min(correlationId.Length, 8))))
                .ToListAsync();

            foreach (var product in testProducts)
            {
                _context.ProductVariants.RemoveRange(product.Variants);
                _context.ProductImages.RemoveRange(product.Images);
            }
            _context.Products.RemoveRange(testProducts);
            cleanupResults.ProductsDeleted = testProducts.Count;

            await _context.SaveChangesAsync();

            // Clean up test users (by email containing correlation ID)
            var testUsers = await _context.Users
                .Where(u => u.Email != null && u.Email.Contains(correlationId))
                .ToListAsync();

            foreach (var user in testUsers)
            {
                await _userManager.DeleteAsync(user);
                cleanupResults.UsersDeleted++;
            }

            return Ok(cleanupResults);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Cleanup failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Seeds sample products for testing. Only works in Development environment.
    /// </summary>
    [HttpPost("seed-products")]
    public async Task<IActionResult> SeedProducts([FromBody] SeedProductsRequest request)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var seededProducts = new List<SeededProduct>();

        // Get or create a test category
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == "air-conditioners");
        if (category == null)
        {
            return BadRequest(new { message = "No categories found. Run database seeding first." });
        }

        for (int i = 0; i < request.Count; i++)
        {
            var sku = $"TEST-{request.CorrelationId?.Substring(0, 8) ?? Guid.NewGuid().ToString("N").Substring(0, 8)}-{i}";
            var slug = $"test-product-{Guid.NewGuid():N}".Substring(0, 40);
            var basePrice = 500m + (i * 100);

            var product = new Product(sku, $"Test Product {i + 1}", slug, basePrice);

            product.SetCategory(category.Id);
            product.SetCompareAtPrice(600m + (i * 100));
            product.SetCostPrice(400m + (i * 100));
            product.SetShortDescription("Test product for E2E testing");
            product.SetDescription("This is a test product created for E2E testing purposes.");
            product.SetMetaTitle($"Test Product {i + 1}");
            product.SetMetaDescription("Test product description");
            product.SetActive(true);
            product.SetFeatured(i < 2);

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Add a variant with stock
            var variant = new ProductVariant(product.Id, $"TEST-VAR-{Guid.NewGuid():N}".Substring(0, 20), "Default");
            variant.SetStockQuantity(request.StockPerProduct);
            _context.ProductVariants.Add(variant);

            seededProducts.Add(new SeededProduct(product.Id, product.Name, product.Slug, product.BasePrice));
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = $"Seeded {request.Count} products", products = seededProducts });
    }

    /// <summary>
    /// Health check for test environment
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(new { status = "healthy", environment = _environment.EnvironmentName });
    }
}

public record ElevateAdminRequest(Guid UserId, string TestSecret);
public record SeedProductsRequest(int Count = 5, int StockPerProduct = 50, string? CorrelationId = null);
public record CleanupResult
{
    public int UsersDeleted { get; set; }
    public int ProductsDeleted { get; set; }
    public int OrdersDeleted { get; set; }
    public int CartsDeleted { get; set; }
    public int ReviewsDeleted { get; set; }
    public int WishlistsDeleted { get; set; }
    public int NotificationsDeleted { get; set; }
}
public record SeededProduct(Guid Id, string Name, string Slug, decimal Price);
#endif
