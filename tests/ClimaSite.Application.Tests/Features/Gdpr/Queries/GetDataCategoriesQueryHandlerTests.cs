using ClimaSite.Application.Features.Gdpr.Queries;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Gdpr.Queries;

public class GetDataCategoriesQueryHandlerTests
{
    private static GetDataCategoriesQueryHandler CreateHandler() => new();

    [Fact]
    public async Task Handle_ReturnsAllDataCategories()
    {
        var result = await CreateHandler().Handle(new GetDataCategoriesQuery(), CancellationToken.None);

        // The handler returns a fixed, transparency-focused catalogue of every data category.
        result.Should().NotBeNull();
        result.Should().HaveCount(11);
    }

    [Fact]
    public async Task Handle_EveryCategoryHasAllTransparencyFieldsPopulated()
    {
        var result = await CreateHandler().Handle(new GetDataCategoriesQuery(), CancellationToken.None);

        // GDPR Article 13/14 requires each category to state purpose, legal basis and retention.
        result.Should().OnlyContain(c =>
            !string.IsNullOrWhiteSpace(c.Category)
            && !string.IsNullOrWhiteSpace(c.Description)
            && !string.IsNullOrWhiteSpace(c.Purpose)
            && !string.IsNullOrWhiteSpace(c.LegalBasis)
            && !string.IsNullOrWhiteSpace(c.RetentionPeriod));
    }

    [Fact]
    public async Task Handle_IncludesKeyCategories_WithExpectedRetention()
    {
        var result = await CreateHandler().Handle(new GetDataCategoriesQuery(), CancellationToken.None);

        result.Should().Contain(c => c.Category == "Account Information");
        // Order history must surface the 7-year legal/tax retention period.
        result.Should().ContainSingle(c => c.Category == "Order History")
            .Which.RetentionPeriod.Should().Contain("7 years");
    }

    [Fact]
    public async Task Handle_ReturnsDistinctCategoryNames()
    {
        var result = await CreateHandler().Handle(new GetDataCategoriesQuery(), CancellationToken.None);

        result.Select(c => c.Category).Should().OnlyHaveUniqueItems();
    }
}
