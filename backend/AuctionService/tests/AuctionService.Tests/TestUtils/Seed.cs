using System;
using AuctionService.Domain.Entities;
using AuctionService.Domain.Enums;

namespace AuctionService.Tests.TestUtils;

public static class Seed
{
    public static Item NewItem(string title = "Art #1") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = "Desc",
        MimeType = "image/png",
        AssetUrl = "https://cdn/x.png",
        Status = ItemStatus.Published,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public static Auction NewScheduledAuction(Item item, DateTimeOffset starts, DateTimeOffset ends) => new()
    {
        Id = Guid.NewGuid(),
        SellerId = Guid.NewGuid(),
        SellerDisplayName = "Seller",
        ItemId = item.Id,
        Item = item,
        StartingPrice = 100m,
        ReservePrice = 150m,
        MinimumBidIncrement = 5m,
        Currency = "USD",
        Status = AuctionStatus.Scheduled,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
        StartsAt = starts,
        EndsAt = ends
    };
}
