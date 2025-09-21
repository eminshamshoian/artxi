using AuctionService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AuctionService.Api.DTOs;

public sealed class CreateAuctionWithNewItemDto
{
    // Ownership
    [Required] public Guid SellerId { get; set; }
    public string? SellerDisplayName { get; set; }

    // Pricing
    [Range(0, double.MaxValue)] public decimal StartingPrice { get; set; } = 0m;
    public decimal? ReservePrice { get; set; }
    public decimal? BuyNowPrice { get; set; }
    [Range(0.01, double.MaxValue)] public decimal MinimumBidIncrement { get; set; } = 1m;
    public string Currency { get; set; } = "USD";

    // Timeline
    [Required] public DateTimeOffset StartsAt { get; set; }
    [Required] public DateTimeOffset EndsAt { get; set; }

    // Optional initial state (usually Draft/Scheduled)
    public AuctionStatus Status { get; set; } = AuctionStatus.Draft;

    // Embedded item creation
    [Required] public CreateItemDto Item { get; set; } = default!;
}
