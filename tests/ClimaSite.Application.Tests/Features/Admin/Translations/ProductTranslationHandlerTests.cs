using ClimaSite.Application.Features.Admin.Translations.Commands;
using ClimaSite.Application.Features.Admin.Translations.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Admin.Translations;

public class AddProductTranslationCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private AddProductTranslationCommandHandler CreateHandler() => new(_context);

    private Product SeedProduct(string sku = "PROD-1")
    {
        var product = new Product(sku, $"Name {sku}", sku.ToLowerInvariant(), 100m);
        _context.AddProduct(product);
        return product;
    }

    [Fact]
    public async Task Handle_AddsTranslation_WhenProductExistsAndNoneForLanguage()
    {
        var product = SeedProduct();

        var id = await CreateHandler().Handle(new AddProductTranslationCommand
        {
            ProductId = product.Id,
            LanguageCode = "de",
            Name = "Deutscher Name",
            ShortDescription = "Kurz",
            Description = "Lang",
            MetaTitle = "Meta",
            MetaDescription = "Meta Desc"
        }, CancellationToken.None);

        id.Should().NotBeEmpty();

        var saved = (await _context.ProductTranslations.ToListAsync())
            .Should().ContainSingle().Subject;
        saved.Id.Should().Be(id);
        saved.ProductId.Should().Be(product.Id);
        saved.LanguageCode.Should().Be("de");
        saved.Name.Should().Be("Deutscher Name");
        saved.ShortDescription.Should().Be("Kurz");
    }

    [Fact]
    public async Task Handle_NormalizesLanguageCodeToLowerInvariant()
    {
        var product = SeedProduct();

        await CreateHandler().Handle(new AddProductTranslationCommand
        {
            ProductId = product.Id,
            LanguageCode = "DE",
            Name = "Name"
        }, CancellationToken.None);

        (await _context.ProductTranslations.FirstAsync()).LanguageCode.Should().Be("de");
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_Throws()
    {
        var act = async () => await CreateHandler().Handle(new AddProductTranslationCommand
        {
            ProductId = Guid.NewGuid(),
            LanguageCode = "de",
            Name = "Name"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_TranslationAlreadyExistsForLanguage_Throws()
    {
        var product = SeedProduct();
        _context.ProductTranslations.Add(new ProductTranslation(product.Id, "de", "Existing"));

        var act = async () => await CreateHandler().Handle(new AddProductTranslationCommand
        {
            ProductId = product.Id,
            LanguageCode = "de",
            Name = "New"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }
}

public class AddProductTranslationCommandValidatorTests
{
    private readonly AddProductTranslationCommandValidator _validator = new();

    private static AddProductTranslationCommand Valid() => new()
    {
        ProductId = Guid.NewGuid(),
        LanguageCode = "de",
        Name = "Valid Name"
    };

    [Fact]
    public void Valid_PassesValidation()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyProductId_Fails()
    {
        var result = _validator.Validate(Valid() with { ProductId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddProductTranslationCommand.ProductId));
    }

    [Fact]
    public void EmptyLanguageCode_Fails()
    {
        var result = _validator.Validate(Valid() with { LanguageCode = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddProductTranslationCommand.LanguageCode));
    }

    [Theory]
    [InlineData("e")]
    [InlineData("eng")]
    public void LanguageCodeWrongLength_Fails(string code)
    {
        var result = _validator.Validate(Valid() with { LanguageCode = code });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddProductTranslationCommand.LanguageCode));
    }

    [Fact]
    public void EmptyName_Fails()
    {
        var result = _validator.Validate(Valid() with { Name = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddProductTranslationCommand.Name));
    }

    [Fact]
    public void NameTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { Name = new string('x', 256) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddProductTranslationCommand.Name));
    }

    [Fact]
    public void ShortDescriptionTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { ShortDescription = new string('x', 501) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddProductTranslationCommand.ShortDescription));
    }

    [Fact]
    public void MetaTitleTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { MetaTitle = new string('x', 201) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddProductTranslationCommand.MetaTitle));
    }

    [Fact]
    public void MetaDescriptionTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { MetaDescription = new string('x', 501) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddProductTranslationCommand.MetaDescription));
    }
}

public class UpdateProductTranslationCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private UpdateProductTranslationCommandHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_UpdatesExistingTranslation_ReturnsTrue()
    {
        var productId = Guid.NewGuid();
        var translation = new ProductTranslation(productId, "de", "Old Name")
        {
            ShortDescription = "old short"
        };
        _context.ProductTranslations.Add(translation);

        var result = await CreateHandler().Handle(new UpdateProductTranslationCommand
        {
            ProductId = productId,
            LanguageCode = "de",
            Name = "New Name",
            ShortDescription = "new short",
            Description = "new desc",
            MetaTitle = "new meta",
            MetaDescription = "new meta desc"
        }, CancellationToken.None);

        result.Should().BeTrue();
        translation.Name.Should().Be("New Name");
        translation.ShortDescription.Should().Be("new short");
        translation.Description.Should().Be("new desc");
        translation.MetaTitle.Should().Be("new meta");
        translation.MetaDescription.Should().Be("new meta desc");
    }

    [Fact]
    public async Task Handle_NormalizesLanguageCodeWhenMatching()
    {
        var productId = Guid.NewGuid();
        _context.ProductTranslations.Add(new ProductTranslation(productId, "de", "Old"));

        var result = await CreateHandler().Handle(new UpdateProductTranslationCommand
        {
            ProductId = productId,
            LanguageCode = "DE",
            Name = "Updated"
        }, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TranslationNotFound_ReturnsFalse()
    {
        var result = await CreateHandler().Handle(new UpdateProductTranslationCommand
        {
            ProductId = Guid.NewGuid(),
            LanguageCode = "de",
            Name = "Whatever"
        }, CancellationToken.None);

        result.Should().BeFalse();
    }
}

public class UpdateProductTranslationCommandValidatorTests
{
    private readonly UpdateProductTranslationCommandValidator _validator = new();

    private static UpdateProductTranslationCommand Valid() => new()
    {
        ProductId = Guid.NewGuid(),
        LanguageCode = "bg",
        Name = "Valid"
    };

    [Fact]
    public void Valid_PassesValidation()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyProductId_Fails()
    {
        var result = _validator.Validate(Valid() with { ProductId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductTranslationCommand.ProductId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("b")]
    [InlineData("bgr")]
    public void InvalidLanguageCode_Fails(string code)
    {
        var result = _validator.Validate(Valid() with { LanguageCode = code });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductTranslationCommand.LanguageCode));
    }

    [Fact]
    public void EmptyName_Fails()
    {
        var result = _validator.Validate(Valid() with { Name = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductTranslationCommand.Name));
    }

    [Fact]
    public void NameTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { Name = new string('x', 256) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductTranslationCommand.Name));
    }
}

public class DeleteProductTranslationCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private DeleteProductTranslationCommandHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_DeletesExistingTranslation_ReturnsTrue()
    {
        var productId = Guid.NewGuid();
        _context.ProductTranslations.Add(new ProductTranslation(productId, "de", "Name"));

        var result = await CreateHandler().Handle(
            new DeleteProductTranslationCommand(productId, "de"), CancellationToken.None);

        result.Should().BeTrue();
        (await _context.ProductTranslations.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NormalizesLanguageCodeWhenMatching()
    {
        var productId = Guid.NewGuid();
        _context.ProductTranslations.Add(new ProductTranslation(productId, "de", "Name"));

        var result = await CreateHandler().Handle(
            new DeleteProductTranslationCommand(productId, "DE"), CancellationToken.None);

        result.Should().BeTrue();
        (await _context.ProductTranslations.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_TranslationNotFound_ReturnsFalse()
    {
        var result = await CreateHandler().Handle(
            new DeleteProductTranslationCommand(Guid.NewGuid(), "de"), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_LeavesOtherLanguagesIntact()
    {
        var productId = Guid.NewGuid();
        _context.ProductTranslations.Add(new ProductTranslation(productId, "de", "German"));
        _context.ProductTranslations.Add(new ProductTranslation(productId, "bg", "Bulgarian"));

        var result = await CreateHandler().Handle(
            new DeleteProductTranslationCommand(productId, "de"), CancellationToken.None);

        result.Should().BeTrue();
        var remaining = await _context.ProductTranslations.ToListAsync();
        remaining.Should().ContainSingle().Which.LanguageCode.Should().Be("bg");
    }
}

public class GetProductTranslationsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetProductTranslationsQueryHandler CreateHandler() => new(_context);

    private Product SeedProduct(string name = "Product Name")
    {
        var product = new Product("PROD-1", name, "product-1", 100m);
        _context.AddProduct(product);
        return product;
    }

    [Fact]
    public async Task Handle_ReturnsTranslationsOrderedByLanguageCode()
    {
        var product = SeedProduct();
        product.Translations.Add(new ProductTranslation(product.Id, "de", "Deutsch"));
        product.Translations.Add(new ProductTranslation(product.Id, "bg", "Български"));

        var result = await CreateHandler().Handle(
            new GetProductTranslationsQuery(product.Id), CancellationToken.None);

        result.ProductId.Should().Be(product.Id);
        result.ProductName.Should().Be("Product Name");
        result.DefaultLanguage.Should().Be("en");
        result.Translations.Should().HaveCount(2);
        result.Translations[0].LanguageCode.Should().Be("bg");
        result.Translations[1].LanguageCode.Should().Be("de");
    }

    [Fact]
    public async Task Handle_NoTranslations_ReportsAllNonEnglishLanguagesAsAvailable()
    {
        var product = SeedProduct();

        var result = await CreateHandler().Handle(
            new GetProductTranslationsQuery(product.Id), CancellationToken.None);

        result.Translations.Should().BeEmpty();
        result.AvailableLanguages.Should().BeEquivalentTo("bg", "de");
    }

    [Fact]
    public async Task Handle_ExistingLanguageRemovedFromAvailableList()
    {
        var product = SeedProduct();
        product.Translations.Add(new ProductTranslation(product.Id, "bg", "Български"));

        var result = await CreateHandler().Handle(
            new GetProductTranslationsQuery(product.Id), CancellationToken.None);

        result.AvailableLanguages.Should().BeEquivalentTo("de");
    }

    [Fact]
    public async Task Handle_ProductNotFound_Throws()
    {
        var act = async () => await CreateHandler().Handle(
            new GetProductTranslationsQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }
}
