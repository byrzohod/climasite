namespace ClimaSite.Application.Features.Wishlist.DTOs;

public class WishlistDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public List<WishlistItemDto> Items { get; init; } = [];
    public int ItemCount { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class WishlistItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSlug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public bool IsOnSale { get; init; }
    public bool InStock { get; init; }
    public DateTime AddedAt { get; init; }
}
