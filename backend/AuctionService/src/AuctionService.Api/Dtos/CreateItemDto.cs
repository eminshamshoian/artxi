using AuctionService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AuctionService.Api.DTOs;

public sealed class CreateItemDto
{
    [Required] public string Title { get; set; } = default!;
    [Required] public string Description { get; set; } = default!;
    public Guid? CreatorId { get; set; }
    public string? CreatorDisplayName { get; set; }
    public string? CollectionName { get; set; }

    public int? EditionNumber { get; set; }
    public int? EditionSize { get; set; }

    public MediaType MediaType { get; set; } = MediaType.Image;

    [Required] public string MimeType { get; set; } = default!;
    [Required] public string AssetUrl { get; set; } = default!;
    public string? PreviewUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long FileSizeBytes { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public string? ChecksumSha256 { get; set; }
    public string? ExternalRef { get; set; }

    public LicenseType License { get; set; } = LicenseType.StandardPersonal;
    [Range(0, 10_000)] public int RoyaltyBps { get; set; } = 0;

    public ItemStatus Status { get; set; } = ItemStatus.Draft;

    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Attributes { get; set; } = new();
}
