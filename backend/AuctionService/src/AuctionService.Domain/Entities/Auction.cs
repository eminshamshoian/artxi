using AuctionService.Domain.Enums;

namespace AuctionService.Domain.Entities;

public sealed class Auction
{
    public Guid Id { get; set; }

    // Ownership
    public required Guid SellerId { get; set; }
    public string? SellerDisplayName { get; set; }

    public Guid? WinnerId { get; set; }
    public string? WinnerDisplayName { get; set; }

    // Catalog link (1:1 with Item)
    public required Guid ItemId { get; set; }
    public required Item Item { get; set; }   // Nav property back to Item

    // Pricing
    public decimal StartingPrice { get; set; } = 0m;
    public decimal? ReservePrice { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public decimal MinimumBidIncrement { get; set; } = 1m;
    public string Currency { get; set; } = "USD";

    // State
    public decimal? CurrentHighBid { get; set; }
    public decimal? SoldAmount { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Draft;

    // Timeline
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    // Optimistic concurrency (EF Core)
    public byte[]? RowVersion { get; set; }
}