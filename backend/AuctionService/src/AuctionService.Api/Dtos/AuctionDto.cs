using AuctionService.Domain.Enums;

namespace AuctionService.Api.DTOs;

public sealed record AuctionDto(
    Guid Id,
    Guid SellerId,
    string? SellerDisplayName,
    Guid? WinnerId,
    string? WinnerDisplayName,
    Guid ItemId,
    ItemDto? Item,                  // include when expanding
    decimal StartingPrice,
    decimal? ReservePrice,
    decimal? BuyNowPrice,
    decimal MinimumBidIncrement,
    string Currency,
    decimal? CurrentHighBid,
    decimal? SoldAmount,
    AuctionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt
);
