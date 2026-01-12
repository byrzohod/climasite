using System.Net.Http.Json;
using Bogus;

namespace ClimaSite.E2E.Infrastructure;

/// <summary>
/// Creates REAL data through the API - NO MOCKING.
/// Each test creates its own isolated data set.
/// </summary>
public class TestDataFactory
{
    private readonly HttpClient _apiClient;
    private readonly Faker _faker = new();
    private readonly Guid _correlationId = Guid.NewGuid();
    private TestUser? _adminUser;

    public Guid CorrelationId => _correlationId;

    public TestDataFactory(HttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Ensures admin authentication is set on the API client.
    /// Creates an admin user if one doesn't exist for this factory.
    /// </summary>
    private async Task EnsureAdminAuthAsync()
    {
        if (_adminUser == null || string.IsNullOrEmpty(_adminUser.Token))
        {
            _adminUser = await CreateAdminUserAsync();
        }

        _apiClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminUser.Token);
    }

    /// <summary>
    /// Creates a real user account through the registration API.
    /// </summary>
    public async Task<TestUser> CreateUserAsync(string? email = null, string? password = null)
    {
        var user = new TestUser
        {
            Email = email ?? $"test_{_correlationId:N}_{_faker.Random.AlphaNumeric(8)}@test.com",
            Password = password ?? "TestPassword123@",
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName()
        };

        // Step 1: Register the user
        var registerResponse = await _apiClient.PostAsJsonAsync("/api/auth/register", new
        {
            email = user.Email,
            password = user.Password,
            firstName = user.FirstName,
            lastName = user.LastName
        });

        if (registerResponse.IsSuccessStatusCode)
        {
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
            user.Id = registerResult?.Id ?? Guid.Empty;
        }

        // Step 2: Login to get token
        var loginResponse = await _apiClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = user.Password
        });

        if (loginResponse.IsSuccessStatusCode)
        {
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            user.Token = loginResult?.AccessToken ?? string.Empty;
            if (user.Id == Guid.Empty)
            {
                user.Id = loginResult?.User?.Id ?? Guid.Empty;
            }
        }

        return user;
    }

    /// <summary>
    /// Creates a real product through the admin API.
    /// </summary>
    public async Task<TestProduct> CreateProductAsync(
        string? name = null,
        decimal? price = null,
        int? stock = null,
        Guid? categoryId = null)
    {
        // Ensure admin auth before creating products
        await EnsureAdminAuthAsync();

        var product = new TestProduct
        {
            Name = name ?? $"Test AC Unit {_faker.Commerce.ProductAdjective()}",
            Description = _faker.Commerce.ProductDescription(),
            Price = price ?? _faker.Random.Decimal(500, 5000),
            Stock = stock ?? _faker.Random.Int(10, 100),
            Sku = $"TEST-{_correlationId.ToString("N").Substring(0, 8)}-{_faker.Random.AlphaNumeric(6)}".ToUpper(),
            CategoryId = categoryId ?? Guid.Empty
        };

        // Get a category if not provided
        if (product.CategoryId == Guid.Empty)
        {
            product.CategoryId = await GetOrCreateCategoryAsync();
        }

        var response = await _apiClient.PostAsJsonAsync("/api/admin/products", new
        {
            name = product.Name,
            sku = product.Sku,
            shortDescription = product.Description,
            description = product.Description,
            basePrice = product.Price,
            stockQuantity = product.Stock,
            categoryId = product.CategoryId,
            isActive = true,
            isFeatured = false,
            specifications = new Dictionary<string, object>
            {
                ["BTU Rating"] = _faker.Random.Int(9000, 24000),
                ["Energy Rating"] = _faker.PickRandom("A++", "A+", "A", "B"),
                ["Refrigerant Type"] = "R32"
            }
        });

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ProductResponse>();
            product.Id = result?.Id ?? Guid.Empty;
            product.Slug = result?.Slug ?? string.Empty;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Product creation failed: {response.StatusCode} - {errorContent}");
        }

        return product;
    }

    /// <summary>
    /// Creates a complete order with products and user.
    /// </summary>
    public async Task<TestOrder> CreateOrderAsync(TestUser? user = null, int productCount = 2)
    {
        user ??= await CreateUserAsync();

        var products = new List<TestProduct>();
        for (int i = 0; i < productCount; i++)
        {
            products.Add(await CreateProductAsync());
        }

        // Add products to cart via API with user authentication
        _apiClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);

        foreach (var product in products)
        {
            if (product.Id == Guid.Empty)
            {
                Console.WriteLine($"Skipping invalid product: Id={product.Id}");
                continue;
            }

            // Add to cart without variantId - let the API pick the default variant
            var cartResponse = await _apiClient.PostAsJsonAsync("/api/cart/items", new
            {
                productId = product.Id,
                quantity = 1
            });

            if (!cartResponse.IsSuccessStatusCode)
            {
                var errorContent = await cartResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Cart add failed for product {product.Id}: {cartResponse.StatusCode} - {errorContent}");
            }
        }

        // Create order from cart with correct address format
        var orderResponse = await _apiClient.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = user.Email,
            customerPhone = _faker.Phone.PhoneNumber("+359########"),
            shippingAddress = new
            {
                firstName = user.FirstName,
                lastName = user.LastName,
                addressLine1 = _faker.Address.StreetAddress(),
                addressLine2 = _faker.Address.SecondaryAddress(),
                city = _faker.Address.City(),
                state = "Sofia",
                postalCode = _faker.Address.ZipCode("####"),
                country = "Bulgaria",
                phone = _faker.Phone.PhoneNumber("+359########")
            },
            shippingMethod = "standard"
        });

        var order = new TestOrder
        {
            User = user,
            Products = products
        };

        if (orderResponse.IsSuccessStatusCode)
        {
            var result = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>();
            order.Id = result?.Id ?? Guid.Empty;
            order.OrderNumber = result?.OrderNumber ?? string.Empty;
            order.TotalAmount = result?.TotalAmount ?? 0;
        }
        else
        {
            // Log the error for debugging
            var errorContent = await orderResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Order creation failed: {orderResponse.StatusCode} - {errorContent}");
        }

        return order;
    }

    /// <summary>
    /// Creates a category if one doesn't exist.
    /// </summary>
    public async Task<Guid> GetOrCreateCategoryAsync(string? name = null)
    {
        name ??= "Air Conditioners";

        // Try to get existing category first (use seeded categories)
        var response = await _apiClient.GetAsync("/api/categories");
        if (response.IsSuccessStatusCode)
        {
            var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
            // Find a category matching name, or use the first available one
            var matchingCategory = categories?.FirstOrDefault(c =>
                c.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (matchingCategory != null)
                return matchingCategory.Id;

            // Use any existing category if no match
            if (categories?.Any() == true)
                return categories.First().Id;
        }

        // Ensure admin auth before creating category
        await EnsureAdminAuthAsync();

        var createResponse = await _apiClient.PostAsJsonAsync("/api/admin/categories", new
        {
            name = $"Test {name}",
            description = $"Test category created for E2E tests",
            isActive = true
        });

        if (createResponse.IsSuccessStatusCode)
        {
            var result = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();
            return result?.Id ?? Guid.Empty;
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Creates an admin user for tests requiring elevated privileges.
    /// </summary>
    public async Task<TestUser> CreateAdminUserAsync()
    {
        var user = await CreateUserAsync();

        // Elevate to admin via test endpoint
        var response = await _apiClient.PostAsJsonAsync("/api/test/elevate-admin", new
        {
            userId = user.Id,
            testSecret = Environment.GetEnvironmentVariable("TEST_ADMIN_SECRET") ?? "test-admin-secret"
        });

        if (response.IsSuccessStatusCode)
        {
            // Re-authenticate to get admin token
            var loginResponse = await _apiClient.PostAsJsonAsync("/api/auth/login", new
            {
                email = user.Email,
                password = user.Password
            });

            if (loginResponse.IsSuccessStatusCode)
            {
                var result = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
                user.Token = result?.AccessToken ?? string.Empty;
                user.IsAdmin = true;
            }
        }

        return user;
    }

    /// <summary>
    /// Cleanup all test data created by this factory instance.
    /// </summary>
    public async Task CleanupAsync()
    {
        try
        {
            await _apiClient.DeleteAsync($"/api/test/cleanup/{_correlationId}");
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}

// Supporting DTOs
public class TestUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

public class TestProduct
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
}

public class TestOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public TestUser User { get; set; } = new();
    public List<TestProduct> Products { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

// API Response DTOs
public record RegisterResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] Guid Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("email")] string Email,
    [property: System.Text.Json.Serialization.JsonPropertyName("firstName")] string FirstName,
    [property: System.Text.Json.Serialization.JsonPropertyName("lastName")] string LastName);
public record AuthResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("accessToken")] string AccessToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("refreshToken")] string RefreshToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("user")] AuthUserResponse User);
public record AuthUserResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] Guid Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("email")] string Email);
public record ProductResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] Guid Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("slug")] string Slug);
public record ProductDetailsResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] Guid Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("slug")] string Slug,
    [property: System.Text.Json.Serialization.JsonPropertyName("variants")] List<ProductVariantResponse>? Variants);
public record ProductVariantResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] Guid Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("sku")] string Sku,
    [property: System.Text.Json.Serialization.JsonPropertyName("name")] string? Name);
public record OrderResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] Guid Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("orderNumber")] string OrderNumber,
    [property: System.Text.Json.Serialization.JsonPropertyName("total")] decimal TotalAmount);
public record CategoryResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] Guid Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("name")] string Name);
