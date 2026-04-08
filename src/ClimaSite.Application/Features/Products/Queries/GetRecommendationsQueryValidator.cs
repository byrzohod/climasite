#nullable enable

using FluentValidation;

namespace ClimaSite.Application.Features.Products.Queries;

public class GetRecommendationsQueryValidator : AbstractValidator<GetRecommendationsQuery>
{
    public GetRecommendationsQueryValidator()
    {
        RuleFor(x => x.AreaM2)
            .InclusiveBetween(5, 500)
            .WithMessage("Area must be between 5 and 500 m²");

        RuleFor(x => x.RoomType)
            .NotEmpty()
            .WithMessage("Room type is required")
            .Must(x => new[] { "living", "bedroom", "office", "commercial" }.Contains(x.ToLowerInvariant()))
            .WithMessage("Room type must be one of: living, bedroom, office, commercial");

        RuleFor(x => x.ClimateZone)
            .NotEmpty()
            .Length(1)
            .WithMessage("Climate zone must be a single character")
            .Must(x => new[] { 'A', 'B', 'C' }.Contains(char.ToUpperInvariant(x[0])))
            .WithMessage("Climate zone must be A, B, or C");

        RuleFor(x => x.LanguageCode)
            .Must(x => x == null || new[] { "en", "bg", "de" }.Contains(x.ToLowerInvariant()))
            .WithMessage("Language code must be en, bg, or de");
    }
}
