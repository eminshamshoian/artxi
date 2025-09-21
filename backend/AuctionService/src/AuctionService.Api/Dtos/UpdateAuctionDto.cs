using AuctionService.Domain.Enums;

namespace AuctionService.Api.DTOs;

public sealed class UpdateAuctionDto
{
    // Timing (only editable before auction starts)
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }

    // Pricing (only editable before auction starts)
    public decimal? StartingPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public decimal? MinimumBidIncrement { get; set; }

    // Optional status changes (limited transitions; see controller rules)
    public AuctionStatus? Status { get; set; }
}
