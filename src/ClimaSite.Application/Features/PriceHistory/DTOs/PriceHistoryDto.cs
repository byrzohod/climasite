namespace ClimaSite.Application.Features.PriceHistory.DTOs;

public record PricePointDto(
    DateTime Date,
    decimal Price,
    decimal? CompareAtPrice,
    string Reason);

public record ProductPriceHistoryDto(
    Guid ProductId,
    string ProductName,
    decimal CurrentPrice,
    decimal? CurrentCompareAtPrice,
    decimal LowestPrice,
    decimal HighestPrice,
    decimal AveragePrice,
    IReadOnlyList<PricePointDto> PricePoints);
