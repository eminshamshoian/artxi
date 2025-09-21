using AutoMapper;
using AutoMapper.QueryableExtensions;
using AuctionService.Api.DTOs;
using AuctionService.Domain.Entities;
using AuctionService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuctionService.Domain.Enums;

namespace AuctionService.Api.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly AuctionDbContext _db;
    private readonly IMapper _mapper;

    public ItemsController(AuctionDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Items.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(i => i.Title.Contains(search) || i.Description.Contains(search));

        var data = await q
            .OrderByDescending(i => i.CreatedAt)
            .ProjectTo<ItemDto>(_mapper.ConfigurationProvider)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemDto>> Get(Guid id)
    {
        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return NotFound();
        return Ok(_mapper.Map<ItemDto>(item));
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create([FromBody] CreateItemDto dto)
    {
        var item = _mapper.Map<Item>(dto);
        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTimeOffset.UtcNow;

        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = item.Id }, _mapper.Map<ItemDto>(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ItemDto>> Update(Guid id, [FromBody] UpdateItemDto dto)
    {
        var item = await _db.Items.Include(i => i.Auction).FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return NotFound();

        // Immutable always via this endpoint (hard guard – ignore any attempt through model-binding hacks)
        // Not mapping: CreatorId, CreatorDisplayName, MediaType, MimeType, AssetUrl, ChecksumSha256,
        // ExternalRef, FileSizeBytes, WidthPx, HeightPx, License, RoyaltyBps, EditionNumber, EditionSize.

        // Apply safe updates
        if (dto.Title is not null) item.Title = dto.Title;
        if (dto.Description is not null) item.Description = dto.Description;
        if (dto.CollectionName is not null) item.CollectionName = dto.CollectionName;
        if (dto.PreviewUrl is not null) item.PreviewUrl = dto.PreviewUrl;
        if (dto.ThumbnailUrl is not null) item.ThumbnailUrl = dto.ThumbnailUrl;

        if (dto.Tags is not null) item.Tags = dto.Tags;
        if (dto.Attributes is not null) item.Attributes = dto.Attributes;

        // Publish action: only Draft -> Published
        if (dto.Publish == true)
        {
            if (item.Status != ItemStatus.Draft)
                return BadRequest("Only Draft items can be published.");
            item.Status = ItemStatus.Published;
            item.PublishedAt ??= DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();

        var updated = await _db.Items.AsNoTracking().FirstAsync(i => i.Id == id);
        return Ok(_mapper.Map<ItemDto>(updated));
    }


    // Block deletion if item is attached to an auction (1:1)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Items.Include(i => i.Auction).FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return NotFound();
        if (item.Auction is not null) return BadRequest("Cannot delete: item is linked to an auction.");
        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
