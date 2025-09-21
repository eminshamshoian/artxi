using AuctionService.Domain.Enums;

namespace AuctionService.Api.DTOs;

public sealed record ItemDto(
    Guid Id,
    string Title,
    string Description,
    Guid? CreatorId,
    string? CreatorDisplayName,
    string? CollectionName,
    int? EditionNumber,
    int? EditionSize,
    MediaType MediaType,
    string MimeType,
    string AssetUrl,
    string? PreviewUrl,
    string? ThumbnailUrl,
    long FileSizeBytes,
    int? WidthPx,
    int? HeightPx,
    string? ChecksumSha256,
    string? ExternalRef,
    LicenseType License,
    int RoyaltyBps,
    ItemStatus Status,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Attributes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt
);