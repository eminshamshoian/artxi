using System.ComponentModel.DataAnnotations;
using AuctionService.Domain.Enums;

namespace AuctionService.Api.DTOs;

public sealed class CreateAuctionWithExistingItemDto
{
    [Required] public Guid SellerId { get; set; }
    public string? SellerDisplayName { get; set; }

    [Required] public Guid ItemId { get; set; }

    public decimal StartingPrice { get; set; } = 0m;
    public decimal? ReservePrice { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public decimal MinimumBidIncrement { get; set; } = 1m;
    public string Currency { get; set; } = "USD";

    [Required] public DateTimeOffset StartsAt { get; set; }
    [Required] public DateTimeOffset EndsAt { get; set; }

    public AuctionStatus Status { get; set; } = AuctionStatus.Draft;
}
