using AutoMapper;
using AutoMapper.QueryableExtensions;
using AuctionService.Api.DTOs;
using AuctionService.Domain.Entities;
using AuctionService.Domain.Enums;
using AuctionService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Api.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly AuctionDbContext _db;
    private readonly IMapper _mapper;

    public AuctionsController(AuctionDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    // GET /api/auctions?status=Live&includeItem=true&page=1&pageSize=20&search=neon
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] AuctionStatus? status,
        [FromQuery] bool includeItem = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Auctions.AsNoTracking();

        if (status.HasValue) q = q.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(a => a.Item.Title.Contains(search) || a.Item.Description.Contains(search));

        if (includeItem)
        {
            var data = await q.Include(a => a.Item)
                .OrderByDescending(a => a.UpdatedAt)
                .ProjectTo<AuctionDto>(_mapper.ConfigurationProvider)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();
            return Ok(data);
        }
        else
        {
            var data = await q.Include(a => a.Item)
                .OrderByDescending(a => a.UpdatedAt)
                .ProjectTo<AuctionListItemDto>(_mapper.ConfigurationProvider)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();
            return Ok(data);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuctionDto>> Get(Guid id)
    {
        var auction = await _db.Auctions.Include(a => a.Item)
            .AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (auction is null) return NotFound();
        return _mapper.Map<AuctionDto>(auction);
    }

    // Create auction + NEW item
    [HttpPost("with-item")]
    public async Task<ActionResult<AuctionDto>> CreateWithNewItem([FromBody] CreateAuctionWithNewItemDto dto)
    {
        if (dto.EndsAt <= dto.StartsAt) return BadRequest("EndsAt must be after StartsAt.");

        var auction = _mapper.Map<Auction>(dto);
        auction.Id = Guid.NewGuid();
        auction.Item.Id = Guid.NewGuid();
        auction.ItemId = auction.Item.Id;

        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();

        var created = await _db.Auctions.Include(a => a.Item)
            .AsNoTracking().FirstAsync(a => a.Id == auction.Id);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, _mapper.Map<AuctionDto>(created));
    }

    // Create auction for EXISTING item
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateWithExistingItem([FromBody] CreateAuctionWithExistingItemDto dto)
    {
        if (dto.EndsAt <= dto.StartsAt) return BadRequest("EndsAt must be after StartsAt.");

        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == dto.ItemId);
        if (item is null) return NotFound($"Item {dto.ItemId} not found.");

        var already = await _db.Auctions.AnyAsync(a => a.ItemId == item.Id);
        if (already) return BadRequest("This item already has an auction.");

        var auction = _mapper.Map<Auction>(dto);
        auction.Id = Guid.NewGuid();
        auction.ItemId = item.Id;
        auction.Item = item;

        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();

        var created = await _db.Auctions.Include(a => a.Item)
            .AsNoTracking().FirstAsync(a => a.Id == auction.Id);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, _mapper.Map<AuctionDto>(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AuctionDto>> Update(Guid id, [FromBody] UpdateAuctionDto dto)
    {
        var auction = await _db.Auctions.Include(a => a.Item).FirstOrDefaultAsync(a => a.Id == id);
        if (auction is null) return NotFound();

        var now = DateTimeOffset.UtcNow;
        var hasStarted = now >= auction.StartsAt;
        var hasEnded   = now >= auction.EndsAt;

        // Hard immutables through this endpoint (ignore if present in payload via model-binding tricks):
        // SellerId, SellerDisplayName, WinnerId, WinnerDisplayName, ItemId, Currency,
        // CurrentHighBid, SoldAmount are NOT editable here.

        // 1) Block edits after end
        if (hasEnded)
            return BadRequest("Auction has ended; it cannot be edited.");

        // 2) Edits before start: allow schedule & pricing tweaks (with simple validation)
        if (!hasStarted)
        {
            if (dto.StartsAt is not null)
            {
                if (dto.StartsAt <= now)
                    return BadRequest("StartsAt must be in the future.");
                auction.StartsAt = dto.StartsAt.Value;
            }

            if (dto.EndsAt is not null)
            {
                var newEndsAt = dto.EndsAt.Value;
                var startsRef = dto.StartsAt ?? auction.StartsAt;
                if (newEndsAt <= startsRef)
                    return BadRequest("EndsAt must be after StartsAt.");
                auction.EndsAt = newEndsAt;
            }

            if (dto.StartingPrice is not null && dto.StartingPrice < 0)
                return BadRequest("StartingPrice cannot be negative.");
            if (dto.ReservePrice   is not null && dto.ReservePrice   < 0)
                return BadRequest("ReservePrice cannot be negative.");
            if (dto.BuyNowPrice    is not null && dto.BuyNowPrice    < 0)
                return BadRequest("BuyNowPrice cannot be negative.");
            if (dto.MinimumBidIncrement is not null && dto.MinimumBidIncrement <= 0)
                return BadRequest("MinimumBidIncrement must be positive.");

            if (dto.StartingPrice is not null)       auction.StartingPrice       = dto.StartingPrice.Value;
            if (dto.ReservePrice is not null)        auction.ReservePrice        = dto.ReservePrice.Value;
            if (dto.BuyNowPrice is not null)         auction.BuyNowPrice         = dto.BuyNowPrice.Value;
            if (dto.MinimumBidIncrement is not null) auction.MinimumBidIncrement = dto.MinimumBidIncrement.Value;
        }
        else
        {
            // 3) Edits after start but before end: timing/pricing are frozen
            if (dto.StartsAt is not null || dto.EndsAt is not null ||
                dto.StartingPrice is not null || dto.ReservePrice is not null ||
                dto.BuyNowPrice is not null || dto.MinimumBidIncrement is not null)
            {
                return BadRequest("Once an auction has started, schedule and pricing cannot be changed.");
            }
        }

        // 4) Status transitions (conservative)
        if (dto.Status is not null)
        {
            var target = dto.Status.Value;
            var current = auction.Status;

            // Allowed:
            // Draft -> Scheduled (before start)
            // Scheduled -> Draft (before start)
            // Scheduled -> Cancelled (before start)
            // Live -> Cancelled (edge-case; typically admin only)
            // Live -> Ended   (should be system-driven; allow? we’ll block to be safe)
            // Ended/Settled: immutable

            if (hasEnded)
                return BadRequest("Ended/Settled auctions cannot change status.");

            bool ok = (current, target) switch
            {
                (AuctionStatus.Draft,     AuctionStatus.Scheduled) => !hasStarted,
                (AuctionStatus.Scheduled, AuctionStatus.Draft)     => !hasStarted,
                (AuctionStatus.Scheduled, AuctionStatus.Cancelled) => !hasStarted,
                (AuctionStatus.Live,      AuctionStatus.Cancelled) => true, // optional: gate with role later
                _ => false
            };

            if (!ok)
                return BadRequest($"Illegal status transition {current} -> {target}.");

            auction.Status = target;
        }

        auction.UpdatedAt = now;
        await _db.SaveChangesAsync();

        var updated = await _db.Auctions.Include(a => a.Item)
            .AsNoTracking().FirstAsync(a => a.Id == id);

        return _mapper.Map<AuctionDto>(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var auction = await _db.Auctions.FirstOrDefaultAsync(a => a.Id == id);
        if (auction is null) return NotFound();

        _db.Auctions.Remove(auction);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
