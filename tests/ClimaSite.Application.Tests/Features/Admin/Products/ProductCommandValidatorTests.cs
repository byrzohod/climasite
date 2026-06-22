using ClimaSite.Application.Features.Admin.Products.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Products;

public class ProductCommandValidatorTests
{
    private readonly MockDbContext _context = new();

    private CreateProductCommandValidator CreateValidator() => new(_context);
    private UpdateProductCommandValidator UpdateValidator() => new(_context);
    private DeleteProductCommandValidator DeleteValidator() => new();

    private static CreateProductCommand ValidCreate() => new()
    {
        Name = "Valid Product",
        Sku = "VALID-SKU",
        BasePrice = 100m
    };

    private static UpdateProductCommand ValidUpdate(Guid id) => new()
    {
        Id = id,
        Name = "Valid Product",
        Sku = "VALID-SKU",
        Slug = "valid-product",
        BasePrice = 100m
    };

    private Category SeedCategory()
    {
        var category = new Category("Air Conditioners", "air-conditioners");
        _context.Categories.Add(category);
        return category;
    }

    private void SeedProductWithSku(string sku)
    {
        // SKU is stored upper-invariant by the entity; the validator compares against upper-invariant.
        var product = new Product(sku, "Existing", "existing", 50m);
        _context.AddProduct(product);
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_ValidCommand_Passes()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Create_EmptyName_Fails()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { Name = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public async Task Create_NameTooLong_Fails()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { Name = new string('a', 256) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public async Task Create_EmptySku_Fails()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { Sku = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Sku));
    }

    [Fact]
    public async Task Create_SkuTooLong_Fails()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { Sku = new string('S', 51) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Sku));
    }

    [Fact]
    public async Task Create_DuplicateSku_Fails()
    {
        SeedProductWithSku("DUPE-SKU");

        var result = await CreateValidator().ValidateAsync(ValidCreate() with { Sku = "dupe-sku" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "SKU already exists");
    }

    [Fact]
    public async Task Create_NegativeBasePrice_Fails()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { BasePrice = -1m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.BasePrice));
    }

    [Fact]
    public async Task Create_NegativeCompareAtPrice_Fails()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { CompareAtPrice = -1m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.CompareAtPrice));
    }

    [Fact]
    public async Task Create_NegativeCostPrice_Fails()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { CostPrice = -1m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.CostPrice));
    }

    [Fact]
    public async Task Create_NegativeWarrantyMonths_Fails()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { WarrantyMonths = -1 });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.WarrantyMonths));
    }

    [Fact]
    public async Task Create_NegativeWeight_Fails()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { WeightKg = -0.1m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.WeightKg));
    }

    [Fact]
    public async Task Create_NonExistentCategory_Fails()
    {
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { CategoryId = Guid.NewGuid() });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Category not found");
    }

    [Fact]
    public async Task Create_ExistingCategory_Passes()
    {
        var category = SeedCategory();
        var result = await CreateValidator().ValidateAsync(ValidCreate() with { CategoryId = category.Id });
        result.IsValid.Should().BeTrue();
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_ValidCommand_Passes()
    {
        var result = await UpdateValidator().ValidateAsync(ValidUpdate(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Update_EmptyId_Fails()
    {
        var result = await UpdateValidator().ValidateAsync(ValidUpdate(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Id));
    }

    [Fact]
    public async Task Update_EmptySlug_Fails()
    {
        var result = await UpdateValidator().ValidateAsync(ValidUpdate(Guid.NewGuid()) with { Slug = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Slug));
    }

    [Fact]
    public async Task Update_DuplicateSkuOnDifferentProduct_Fails()
    {
        SeedProductWithSku("TAKEN-SKU");

        var result = await UpdateValidator().ValidateAsync(
            ValidUpdate(Guid.NewGuid()) with { Sku = "taken-sku" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "SKU already exists");
    }

    [Fact]
    public async Task Update_SameSkuOnSameProduct_Passes()
    {
        // The product keeps its own SKU; the uniqueness rule excludes the current Id.
        var product = new Product("OWN-SKU", "Existing", "existing", 50m);
        _context.AddProduct(product);

        var result = await UpdateValidator().ValidateAsync(
            ValidUpdate(product.Id) with { Sku = "own-sku" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Update_NonExistentCategory_Fails()
    {
        var result = await UpdateValidator().ValidateAsync(
            ValidUpdate(Guid.NewGuid()) with { CategoryId = Guid.NewGuid() });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Category not found");
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_ValidId_Passes()
    {
        var result = await DeleteValidator().ValidateAsync(new DeleteProductCommand { Id = Guid.NewGuid() });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_EmptyId_Fails()
    {
        var result = await DeleteValidator().ValidateAsync(new DeleteProductCommand { Id = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DeleteProductCommand.Id));
    }
}
