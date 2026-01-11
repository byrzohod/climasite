using ClimaSite.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Infrastructure.Data;

public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
            await SeedRolesAsync();
            await SeedAdminUserAsync();
            await SeedCategoriesAsync();
            await SeedProductsAsync();
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[] { "Admin", "Customer" };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = role, NormalizedName = role.ToUpperInvariant() });
                _logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@climasite.local";
        const string adminPassword = "Admin123!";

        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator"
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                _logger.LogWarning("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedCategoriesAsync()
    {
        if (await _context.Categories.AnyAsync())
        {
            _logger.LogInformation("Categories already seeded, skipping...");
            return;
        }

        var categories = new List<Category>
        {
            CreateCategory("Air Conditioners", "air-conditioners", "Efficient cooling solutions for homes and businesses", 1, "snowflake"),
            CreateCategory("Heating Systems", "heating-systems", "Reliable heating solutions for all seasons", 2, "fire"),
            CreateCategory("Ventilation", "ventilation", "Fresh air circulation and ventilation systems", 3, "wind"),
            CreateCategory("Accessories", "accessories", "HVAC accessories and replacement parts", 4, "tools")
        };

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();

        // Add subcategories
        var acCategory = categories[0];
        var heatingCategory = categories[1];

        var subCategories = new List<Category>
        {
            CreateSubCategory("Split Air Conditioners", "split-air-conditioners", acCategory.Id, "Wall-mounted split AC units", 1),
            CreateSubCategory("Window Air Conditioners", "window-air-conditioners", acCategory.Id, "Window-mounted AC units", 2),
            CreateSubCategory("Portable Air Conditioners", "portable-air-conditioners", acCategory.Id, "Mobile cooling solutions", 3),
            CreateSubCategory("Central Air Conditioning", "central-air-conditioning", acCategory.Id, "Whole-house cooling systems", 4),
            CreateSubCategory("Heat Pumps", "heat-pumps", heatingCategory.Id, "Energy-efficient heating and cooling", 1),
            CreateSubCategory("Electric Heaters", "electric-heaters", heatingCategory.Id, "Electric space heaters", 2),
            CreateSubCategory("Gas Furnaces", "gas-furnaces", heatingCategory.Id, "Natural gas heating systems", 3),
            CreateSubCategory("Radiators", "radiators", heatingCategory.Id, "Radiator heating systems", 4)
        };

        _context.Categories.AddRange(subCategories);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} categories", categories.Count + subCategories.Count);
    }

    private static Category CreateCategory(string name, string slug, string description, int sortOrder, string icon)
    {
        var category = new Category(name, slug, description);
        category.SetIcon(icon);
        category.SetSortOrder(sortOrder);
        return category;
    }

    private static Category CreateSubCategory(string name, string slug, Guid parentId, string description, int sortOrder)
    {
        var category = new Category(name, slug, description);
        category.SetParent(parentId);
        category.SetSortOrder(sortOrder);
        return category;
    }

    private async Task SeedProductsAsync()
    {
        if (await _context.Products.AnyAsync())
        {
            _logger.LogInformation("Products already seeded, skipping...");
            return;
        }

        var categories = await _context.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id);

        var products = new List<Product>
        {
            // Split Air Conditioners
            CreateProduct("DualZone Pro 12000", "DUALZONE-PRO-12K", "dualzone-pro-12000", 899.99m, 1099.99m,
                categories.GetValueOrDefault("split-air-conditioners"),
                "DualZone", "DZP-12000",
                "High-efficiency inverter split air conditioner with smart home integration",
                "The DualZone Pro 12000 combines cutting-edge inverter technology with whisper-quiet operation. Perfect for bedrooms and living rooms up to 550 sq ft. Features WiFi connectivity for smart home control and energy-saving scheduling.",
                new Dictionary<string, object>
                {
                    { "BTU", 12000 },
                    { "SEER Rating", 21 },
                    { "Room Size", "Up to 550 sq ft" },
                    { "Noise Level", "22 dB" },
                    { "Refrigerant", "R-32" }
                },
                new List<ProductFeature>
                {
                    new("Inverter Technology", "Variable speed compressor for optimal efficiency"),
                    new("Smart Control", "WiFi-enabled with mobile app support"),
                    new("Ultra Quiet", "Only 22 dB - quieter than a whisper"),
                    new("Self-Cleaning", "Automatic cleaning function for fresh air")
                },
                new[] { "inverter", "smart", "quiet", "energy-efficient" },
                true, true, 24, 32.5m),

            CreateProduct("ArcticBreeze 9000", "ARCTIC-BREEZE-9K", "arcticbreeze-9000", 649.99m, null,
                categories.GetValueOrDefault("split-air-conditioners"),
                "ArcticBreeze", "AB-9000",
                "Compact split AC perfect for small to medium rooms",
                "The ArcticBreeze 9000 delivers powerful cooling in a compact design. Ideal for bedrooms, home offices, and small living spaces up to 400 sq ft.",
                new Dictionary<string, object>
                {
                    { "BTU", 9000 },
                    { "SEER Rating", 18 },
                    { "Room Size", "Up to 400 sq ft" },
                    { "Noise Level", "26 dB" },
                    { "Refrigerant", "R-410A" }
                },
                new List<ProductFeature>
                {
                    new("Turbo Mode", "Rapid cooling in just 3 minutes"),
                    new("Sleep Mode", "Automatic temperature adjustment at night"),
                    new("Dehumidifier", "Built-in dehumidification function")
                },
                new[] { "compact", "bedroom", "efficient" },
                true, false, 24, 28.0m),

            CreateProduct("CoolMaster 18000", "COOLMASTER-18K", "coolmaster-18000", 1299.99m, 1499.99m,
                categories.GetValueOrDefault("split-air-conditioners"),
                "CoolMaster", "CM-18000",
                "Powerful split AC for large rooms and open spaces",
                "The CoolMaster 18000 provides commercial-grade cooling power for large residential spaces. Perfect for living rooms, open-plan areas, and small offices up to 850 sq ft.",
                new Dictionary<string, object>
                {
                    { "BTU", 18000 },
                    { "SEER Rating", 19 },
                    { "Room Size", "Up to 850 sq ft" },
                    { "Noise Level", "28 dB" },
                    { "Refrigerant", "R-32" }
                },
                new List<ProductFeature>
                {
                    new("3D Air Flow", "Multi-directional air distribution"),
                    new("Plasma Filter", "Advanced air purification"),
                    new("Auto Restart", "Automatic restart after power outage")
                },
                new[] { "powerful", "large-room", "commercial-grade" },
                true, false, 36, 45.0m),

            // Heat Pumps
            CreateProduct("EcoHeat Plus 24", "ECOHEAT-PLUS-24", "ecoheat-plus-24", 2499.99m, 2899.99m,
                categories.GetValueOrDefault("heat-pumps"),
                "EcoHeat", "EHP-24000",
                "High-efficiency heat pump for year-round comfort",
                "The EcoHeat Plus 24 provides both heating and cooling with exceptional efficiency. Operates down to -15°F and delivers consistent comfort in all seasons.",
                new Dictionary<string, object>
                {
                    { "BTU Cooling", 24000 },
                    { "BTU Heating", 26000 },
                    { "SEER Rating", 22 },
                    { "HSPF", 10.5 },
                    { "Operating Range", "-15°F to 115°F" }
                },
                new List<ProductFeature>
                {
                    new("Dual Mode", "Both heating and cooling in one unit"),
                    new("Cold Climate", "Works down to -15°F"),
                    new("Defrost Mode", "Intelligent automatic defrost"),
                    new("Zoned Control", "Control multiple zones independently")
                },
                new[] { "heat-pump", "efficient", "all-season", "inverter" },
                true, true, 60, 68.0m),

            CreateProduct("ThermoFlex Mini", "THERMOFLEX-MINI", "thermoflex-mini", 1599.99m, null,
                categories.GetValueOrDefault("heat-pumps"),
                "ThermoFlex", "TFM-12000",
                "Compact heat pump for smaller spaces",
                "The ThermoFlex Mini delivers efficient heating and cooling for smaller spaces. Perfect for apartments, studios, and additions up to 500 sq ft.",
                new Dictionary<string, object>
                {
                    { "BTU Cooling", 12000 },
                    { "BTU Heating", 13000 },
                    { "SEER Rating", 20 },
                    { "HSPF", 9.5 },
                    { "Operating Range", "-5°F to 110°F" }
                },
                new List<ProductFeature>
                {
                    new("Compact Design", "Space-saving outdoor unit"),
                    new("Quick Install", "DIY-friendly installation option"),
                    new("Energy Star", "Energy Star certified for savings")
                },
                new[] { "compact", "heat-pump", "energy-star" },
                false, false, 48, 42.0m),

            // Electric Heaters
            CreateProduct("RadiantMax Pro", "RADIANTMAX-PRO", "radiantmax-pro", 349.99m, 449.99m,
                categories.GetValueOrDefault("electric-heaters"),
                "RadiantMax", "RMP-2000",
                "Premium infrared radiant heater with smart features",
                "The RadiantMax Pro uses advanced infrared technology for efficient, comfortable heating. Features smart thermostat and scheduling via mobile app.",
                new Dictionary<string, object>
                {
                    { "Wattage", 2000 },
                    { "Coverage", "Up to 300 sq ft" },
                    { "Heating Type", "Infrared" },
                    { "Safety", "Tip-over protection, overheat shutoff" }
                },
                new List<ProductFeature>
                {
                    new("Infrared Heat", "Natural warmth like sunlight"),
                    new("Smart Control", "WiFi-enabled with app control"),
                    new("Silent Operation", "No fan, no noise"),
                    new("Portable", "Easy to move between rooms")
                },
                new[] { "infrared", "smart", "portable", "silent" },
                true, false, 12, 8.5m),

            CreateProduct("ConvectAir 1500", "CONVECTAIR-1500", "convectair-1500", 199.99m, null,
                categories.GetValueOrDefault("electric-heaters"),
                "ConvectAir", "CA-1500",
                "Efficient convection heater for everyday use",
                "The ConvectAir 1500 provides gentle, even heating using natural convection. Perfect for bedrooms, offices, and living spaces.",
                new Dictionary<string, object>
                {
                    { "Wattage", 1500 },
                    { "Coverage", "Up to 200 sq ft" },
                    { "Heating Type", "Convection" },
                    { "Safety", "Cool-touch housing, auto shutoff" }
                },
                new List<ProductFeature>
                {
                    new("Even Heating", "Gentle warmth throughout the room"),
                    new("Digital Thermostat", "Precise temperature control"),
                    new("Timer", "Programmable 24-hour timer")
                },
                new[] { "convection", "bedroom", "affordable" },
                true, false, 12, 6.0m),

            // Portable Air Conditioners
            CreateProduct("PortaCool 14000", "PORTACOOL-14K", "portacool-14000", 699.99m, 799.99m,
                categories.GetValueOrDefault("portable-air-conditioners"),
                "PortaCool", "PC-14000",
                "High-capacity portable AC with dual hose design",
                "The PortaCool 14000 delivers powerful cooling with the flexibility of portability. Dual-hose design ensures maximum efficiency without negative pressure.",
                new Dictionary<string, object>
                {
                    { "BTU", 14000 },
                    { "Coverage", "Up to 600 sq ft" },
                    { "Hose Type", "Dual hose" },
                    { "Dehumidification", "80 pints/day" }
                },
                new List<ProductFeature>
                {
                    new("Dual Hose", "More efficient than single-hose models"),
                    new("3-in-1", "AC, fan, and dehumidifier"),
                    new("Easy Rolling", "Smooth-glide casters"),
                    new("Self-Evaporating", "No bucket to empty")
                },
                new[] { "portable", "dual-hose", "versatile" },
                true, true, 24, 38.0m),

            CreateProduct("MobileChill 10000", "MOBILECHILL-10K", "mobilechill-10000", 449.99m, null,
                categories.GetValueOrDefault("portable-air-conditioners"),
                "MobileChill", "MC-10000",
                "Compact portable AC for smaller spaces",
                "The MobileChill 10000 is perfect for bedrooms, apartments, and home offices. Compact size with powerful performance.",
                new Dictionary<string, object>
                {
                    { "BTU", 10000 },
                    { "Coverage", "Up to 450 sq ft" },
                    { "Hose Type", "Single hose" },
                    { "Dehumidification", "52 pints/day" }
                },
                new List<ProductFeature>
                {
                    new("Compact Size", "Fits in tight spaces"),
                    new("Remote Control", "Full-function remote included"),
                    new("Sleep Mode", "Quiet nighttime operation")
                },
                new[] { "portable", "compact", "bedroom" },
                true, false, 12, 29.0m),

            // Ventilation
            CreateProduct("FreshAir ERV 200", "FRESHAIR-ERV-200", "freshair-erv-200", 899.99m, 999.99m,
                categories.GetValueOrDefault("ventilation"),
                "FreshAir", "ERV-200",
                "Energy recovery ventilator for whole-home fresh air",
                "The FreshAir ERV 200 brings fresh outdoor air while recovering energy from exhaust air. Maintains comfort while improving indoor air quality.",
                new Dictionary<string, object>
                {
                    { "CFM", 200 },
                    { "Energy Recovery", "Up to 80%" },
                    { "Coverage", "Up to 2500 sq ft" },
                    { "Filter Type", "MERV 13" }
                },
                new List<ProductFeature>
                {
                    new("Energy Recovery", "Saves up to 80% of heating/cooling energy"),
                    new("Fresh Air", "Continuous fresh air supply"),
                    new("Humidity Control", "Helps control indoor humidity"),
                    new("Quiet Operation", "Low noise design")
                },
                new[] { "erv", "fresh-air", "energy-recovery", "whole-home" },
                true, true, 36, 28.0m),

            CreateProduct("VentMax Exhaust Pro", "VENTMAX-EXHAUST", "ventmax-exhaust-pro", 189.99m, null,
                categories.GetValueOrDefault("ventilation"),
                "VentMax", "VEP-110",
                "Professional-grade bathroom exhaust fan",
                "The VentMax Exhaust Pro provides powerful moisture removal with ultra-quiet operation. Features humidity sensor for automatic operation.",
                new Dictionary<string, object>
                {
                    { "CFM", 110 },
                    { "Noise Level", "0.8 sones" },
                    { "Energy Star", true },
                    { "Sensor", "Humidity sensing" }
                },
                new List<ProductFeature>
                {
                    new("Humidity Sensor", "Automatic on/off based on moisture"),
                    new("Ultra Quiet", "Less than 1 sone"),
                    new("LED Light", "Built-in LED lighting"),
                    new("Energy Star", "Energy Star certified")
                },
                new[] { "bathroom", "exhaust", "humidity-sensor", "quiet" },
                false, false, 24, 3.5m),

            // Accessories
            CreateProduct("SmartThermo Pro", "SMARTTHERMO-PRO", "smartthermo-pro", 249.99m, 299.99m,
                categories.GetValueOrDefault("accessories"),
                "SmartThermo", "STP-100",
                "WiFi-enabled smart thermostat with learning capability",
                "The SmartThermo Pro learns your schedule and preferences to optimize comfort and savings. Compatible with most HVAC systems.",
                new Dictionary<string, object>
                {
                    { "Display", "Color touchscreen" },
                    { "Compatibility", "Most 24V systems" },
                    { "Sensors", "Temperature, humidity, motion" },
                    { "Smart Home", "Works with Alexa, Google, HomeKit" }
                },
                new List<ProductFeature>
                {
                    new("Learning", "Learns your schedule automatically"),
                    new("Voice Control", "Works with major voice assistants"),
                    new("Energy Reports", "Weekly energy saving reports"),
                    new("Remote Sensors", "Optional room sensors available")
                },
                new[] { "smart", "thermostat", "wifi", "learning" },
                true, true, 24, 0.5m),

            CreateProduct("PureAir HEPA Filter Pack", "PUREAIR-HEPA-3PK", "pureair-hepa-filter-pack", 79.99m, 99.99m,
                categories.GetValueOrDefault("accessories"),
                "PureAir", "HEPA-3PK",
                "Premium HEPA replacement filters - 3 pack",
                "High-quality HEPA replacement filters compatible with most standard air purifiers and HVAC systems. Captures 99.97% of particles.",
                new Dictionary<string, object>
                {
                    { "Filter Type", "True HEPA" },
                    { "Efficiency", "99.97% at 0.3 microns" },
                    { "Pack Size", 3 },
                    { "Replacement Interval", "6-12 months" }
                },
                new List<ProductFeature>
                {
                    new("True HEPA", "Captures 99.97% of particles"),
                    new("Pre-filter", "Includes activated carbon pre-filter"),
                    new("Long Lasting", "6-12 months per filter")
                },
                new[] { "hepa", "filter", "air-quality", "replacement" },
                false, false, 0, 1.5m),

            CreateProduct("CoolLine Copper Tubing Kit", "COOLLINE-COPPER-25", "coolline-copper-tubing-kit", 149.99m, null,
                categories.GetValueOrDefault("accessories"),
                "CoolLine", "CTK-25",
                "25ft copper refrigerant line set with insulation",
                "Professional-grade copper tubing kit for mini-split installations. Includes pre-flared connections, insulation, and communication wire.",
                new Dictionary<string, object>
                {
                    { "Length", "25 feet" },
                    { "Line Sizes", "1/4\" liquid, 3/8\" suction" },
                    { "Insulation", "Included" },
                    { "Communication Wire", "14/4 gauge included" }
                },
                new List<ProductFeature>
                {
                    new("Pre-Flared", "Factory flared connections"),
                    new("Insulated", "Pre-insulated for efficiency"),
                    new("Complete Kit", "All components included")
                },
                new[] { "copper", "tubing", "installation", "mini-split" },
                true, false, 0, 12.0m)
        };

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Add variants and images for each product
        foreach (var product in products)
        {
            await AddProductVariantsAndImages(product);
        }

        _logger.LogInformation("Seeded {Count} products", products.Count);
    }

    private static Product CreateProduct(
        string name, string sku, string slug, decimal basePrice, decimal? compareAtPrice,
        Guid? categoryId, string brand, string model,
        string shortDescription, string description,
        Dictionary<string, object> specifications,
        List<ProductFeature> features,
        string[] tags,
        bool isActive, bool isFeatured, int warrantyMonths, decimal weightKg)
    {
        var product = new Product(sku, name, slug, basePrice);
        if (compareAtPrice.HasValue)
            product.SetCompareAtPrice(compareAtPrice);
        if (categoryId.HasValue)
            product.SetCategory(categoryId);
        product.SetBrand(brand);
        product.SetModel(model);
        product.SetShortDescription(shortDescription);
        product.SetDescription(description);
        product.SetSpecifications(specifications);
        product.SetFeatures(features);
        product.SetTags(new List<string>(tags));
        product.SetActive(isActive);
        product.SetFeatured(isFeatured);
        product.SetWarrantyMonths(warrantyMonths);
        product.SetWeightKg(weightKg);
        return product;
    }

    private async Task AddProductVariantsAndImages(Product product)
    {
        // Add default variant
        var defaultVariant = new ProductVariant(product.Id, $"{product.Sku}-DEFAULT", "Standard");
        defaultVariant.SetStockQuantity(new Random().Next(5, 100));
        _context.ProductVariants.Add(defaultVariant);

        // Add a primary image (using placeholder)
        var image = new ProductImage(product.Id, $"https://placehold.co/600x400/0d9488/ffffff?text={Uri.EscapeDataString(product.Name)}");
        image.SetAltText(product.Name);
        image.SetPrimary(true);
        _context.ProductImages.Add(image);

        await _context.SaveChangesAsync();
    }
}
