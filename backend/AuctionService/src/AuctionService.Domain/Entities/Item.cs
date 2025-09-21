using System.ComponentModel.DataAnnotations.Schema;
using AuctionService.Domain.Enums;

namespace AuctionService.Domain.Entities;

[Table("Items")]
public sealed class Item
{
    public Guid Id { get; set; }

    // Core metadata
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Guid? CreatorId { get; set; }
    public string? CreatorDisplayName { get; set; }
    public string? CollectionName { get; set; }

    // Editions
    public int? EditionNumber { get; set; }
    public int? EditionSize { get; set; }

    // Media
    public MediaType MediaType { get; set; } = MediaType.Image;
    public required string MimeType { get; set; }
    public required string AssetUrl { get; set; }
    public string? PreviewUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long FileSizeBytes { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public string? ChecksumSha256 { get; set; }
    public string? ExternalRef { get; set; }

    // Rights
    public LicenseType License { get; set; } = LicenseType.StandardPersonal;
    public int RoyaltyBps { get; set; }

    // Catalog
    public ItemStatus Status { get; set; } = ItemStatus.Draft;
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Attributes { get; set; } = new();

    // Audit
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }

    // Navigation back to Auction
    public Auction? Auction { get; set; }
}
