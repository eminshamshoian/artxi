namespace AuctionService.Api.DTOs;

public sealed class UpdateItemDto
{
    // Editable display metadata only
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? CollectionName { get; set; }

    // Media *previews* are ok to tweak for UX; core media is immutable here
    public string? PreviewUrl { get; set; }
    public string? ThumbnailUrl { get; set; }

    // Catalog
    public List<string>? Tags { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }

    // Optional publish action (only Draft -> Published)
    public bool? Publish { get; set; }
}
