using AuctionService.Domain.Entities;
using AuctionService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.Infrastructure.Data;

public static class DbInitializer
{
    public static void InitDb(IServiceProvider services, bool seedSampleData)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

        db.Database.Migrate();

        if (!seedSampleData) return;
        if (db.Auctions.Any() || db.Items.Any()) return;

        var now = DateTimeOffset.UtcNow;

        // ----- Items (digital artwork metadata) -----
        var item1 = new Item
        {
            Id = Guid.NewGuid(),
            Title = "Neon City #001",
            Description = "A 1/1 cyberpunk skyline rendered in ray-traced neon.",
            CreatorId = Guid.NewGuid(),
            CreatorDisplayName = "Aria Nova",
            CollectionName = "Neon City",
            EditionNumber = null,
            EditionSize   = null,
            MediaType = MediaType.Image,
            MimeType = "image/png",
            AssetUrl = "https://cdn.example.com/art/neon-city-001.png",
            PreviewUrl = "https://cdn.example.com/art/preview/neon-city-001.png",
            ThumbnailUrl = "https://cdn.example.com/art/thumb/neon-city-001.png",
            FileSizeBytes = 3_456_789,
            WidthPx = 4000,
            HeightPx = 2500,
            ChecksumSha256 = "c0ffee...001",
            ExternalRef = "ipfs://bafy...001",
            License = LicenseType.StandardPersonal,
            RoyaltyBps = 750,
            Status = ItemStatus.Published,
            Tags = new() { "cyberpunk", "city", "neon", "1of1" },
            Attributes = new()
            {
                ["Palette"] = "Neon",
                ["Style"] = "Ray Tracing",
                ["Mood"] = "Futuristic"
            },
            CreatedAt = now.AddDays(-3),
            PublishedAt = now.AddDays(-2)
        };

        var item2 = new Item
        {
            Id = Guid.NewGuid(),
            Title = "Generative Bloom #12/25",
            Description = "Procedural flowers generated from Perlin noise.",
            CreatorId = Guid.NewGuid(),
            CreatorDisplayName = "AlgoBloom",
            CollectionName = "Generative Bloom",
            EditionNumber = 12,
            EditionSize   = 25,
            MediaType = MediaType.Animation,
            MimeType = "video/mp4",
            AssetUrl = "https://cdn.example.com/art/generative-bloom-12.mp4",
            PreviewUrl = "https://cdn.example.com/art/preview/generative-bloom-12.mp4",
            ThumbnailUrl = "https://cdn.example.com/art/thumb/generative-bloom-12.png",
            FileSizeBytes = 12_345_678,
            WidthPx = 1920,
            HeightPx = 1080,
            ChecksumSha256 = "c0ffee...012",
            ExternalRef = "ipfs://bafy...012",
            License = LicenseType.CommercialLimited,
            RoyaltyBps = 500,
            Status = ItemStatus.Published,
            Tags = new() { "generative", "flowers", "animation" },
            Attributes = new()
            {
                ["Algorithm"] = "Perlin",
                ["Edition"] = "12/25"
            },
            CreatedAt = now.AddDays(-10),
            PublishedAt = now.AddDays(-9)
        };

        var item3 = new Item
        {
            Id = Guid.NewGuid(),
            Title = "Synthwave Horizon",
            Description = "Looping audio-visual piece with analog synth textures.",
            CreatorId = Guid.NewGuid(),
            CreatorDisplayName = "LumenField",
            CollectionName = "Horizons",
            MediaType = MediaType.Audio,
            MimeType = "audio/mpeg",
            AssetUrl = "https://cdn.example.com/art/synthwave-horizon.mp3",
            PreviewUrl = "https://cdn.example.com/art/preview/synthwave-horizon.mp3",
            ThumbnailUrl = "https://cdn.example.com/art/thumb/synthwave-horizon.png",
            FileSizeBytes = 8_765_432,
            WidthPx = null,
            HeightPx = null,
            ChecksumSha256 = "c0ffee...099",
            ExternalRef = null,
            License = LicenseType.StandardPersonal,
            RoyaltyBps = 1000,
            Status = ItemStatus.Published,
            Tags = new() { "audio", "loop", "synthwave" },
            Attributes = new()
            {
                ["Key"] = "A minor",
                ["BPM"] = "92"
            },
            CreatedAt = now.AddDays(-5),
            PublishedAt = now.AddDays(-4)
        };

        var item4 = new Item
        {
            Id = Guid.NewGuid(),
            Title = "Voxel Fox",
            Description = "3D voxel character ready for game engines.",
            CreatorId = Guid.NewGuid(),
            CreatorDisplayName = "VoxKid",
            CollectionName = "Voxel Menagerie",
            MediaType = MediaType.ThreeD,
            MimeType = "model/gltf-binary",
            AssetUrl = "https://cdn.example.com/art/voxel-fox.glb",
            PreviewUrl = "https://cdn.example.com/art/preview/voxel-fox.glb",
            ThumbnailUrl = "https://cdn.example.com/art/thumb/voxel-fox.png",
            FileSizeBytes = 2_222_222,
            WidthPx = null,
            HeightPx = null,
            License = LicenseType.CommercialLimited,
            RoyaltyBps = 600,
            Status = ItemStatus.Published,
            Tags = new() { "3d", "voxel", "character" },
            Attributes = new()
            {
                ["Rigged"] = "True",
                ["Polygons"] = "Low"
            },
            CreatedAt = now.AddDays(-1),
            PublishedAt = now
        };

        var items = new[] { item1, item2, item3, item4 };

        // ----- Auctions (1:1 with Items; FK lives on Auction.ItemId) -----
        var auction1 = new Auction
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            SellerDisplayName = "Aria Nova",
            ItemId = item1.Id,
            Item = item1,
            StartingPrice = 250m,
            ReservePrice  = 500m,
            BuyNowPrice   = 1200m,
            MinimumBidIncrement = 25m,
            Currency = "USD",
            Status = AuctionStatus.Live,
            CurrentHighBid = 350m,
            SoldAmount = null,
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now,
            StartsAt = now.AddMinutes(-30),
            EndsAt   = now.AddDays(3)
        };

        var auction2 = new Auction
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            SellerDisplayName = "AlgoBloom",
            ItemId = item2.Id,
            Item = item2,
            StartingPrice = 100m,
            ReservePrice  = 300m,
            BuyNowPrice   = null,
            MinimumBidIncrement = 10m,
            Currency = "USD",
            Status = AuctionStatus.Scheduled,
            CurrentHighBid = null,
            SoldAmount = null,
            CreatedAt = now.AddDays(-8),
            UpdatedAt = now.AddDays(-1),
            StartsAt = now.AddHours(4),
            EndsAt   = now.AddDays(2)
        };

        var auction3 = new Auction
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            SellerDisplayName = "LumenField",
            ItemId = item3.Id,
            Item = item3,
            StartingPrice = 50m,
            ReservePrice  = 150m,
            BuyNowPrice   = 500m,
            MinimumBidIncrement = 5m,
            Currency = "USD",
            Status = AuctionStatus.Ended, // simulate ended without sale
            CurrentHighBid = 120m,
            SoldAmount = null,            // reserve not met
            CreatedAt = now.AddDays(-4),
            UpdatedAt = now.AddDays(-1),
            StartsAt = now.AddDays(-3),
            EndsAt   = now.AddHours(-2)
        };

        var auction4 = new Auction
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            SellerDisplayName = "VoxKid",
            WinnerId = Guid.NewGuid(),
            WinnerDisplayName = "Collector42",
            ItemId = item4.Id,
            Item = item4,
            StartingPrice = 75m,
            ReservePrice  = null,         // no reserve
            BuyNowPrice   = null,
            MinimumBidIncrement = 5m,
            Currency = "USD",
            Status = AuctionStatus.Settled, // simulate sold + settled
            CurrentHighBid = 260m,
            SoldAmount = 260m,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now,
            StartsAt = now.AddHours(-12),
            EndsAt   = now.AddHours(-1)
        };

        db.Items.AddRange(items);
        db.Auctions.AddRange(auction1, auction2, auction3, auction4);

        db.SaveChanges();
    }
}
