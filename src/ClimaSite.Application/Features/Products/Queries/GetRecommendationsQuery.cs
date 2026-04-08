#nullable enable

using ClimaSite.Application.Features.Products.DTOs;
using MediatR;

namespace ClimaSite.Application.Features.Products.Queries;

public record GetRecommendationsQuery : IRequest<List<RecommendedProductDto>>
{
    /// <summary>
    /// Room/space area in square meters. Valid range: 5-500 m².
    /// </summary>
    public int AreaM2 { get; init; }

    /// <summary>
    /// Room type: living, bedroom, office, commercial.
    /// </summary>
    public string RoomType { get; init; } = string.Empty;

    /// <summary>
    /// Climate zone: A (coastal/warm), B (temperate), C (alpine/cold).
    /// </summary>
    public string ClimateZone { get; init; } = "B";

    /// <summary>
    /// Optional language code for translations (en, bg, de).
    /// </summary>
    public string? LanguageCode { get; init; }
}
