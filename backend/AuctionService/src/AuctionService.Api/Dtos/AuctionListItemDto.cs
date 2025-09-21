using AuctionService.Domain.Enums;

namespace AuctionService.Api.DTOs;

public sealed class AuctionListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;      // from Item
    public string? SellerDisplayName { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal? CurrentHighBid { get; set; }
    public AuctionStatus Status { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string? ThumbnailUrl { get; set; }              // from Item
}