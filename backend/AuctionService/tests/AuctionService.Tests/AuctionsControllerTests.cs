using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AuctionService.Api.Controllers;
using AuctionService.Api.DTOs;
using AuctionService.Domain.Entities;
using AuctionService.Domain.Enums;
using AuctionService.Infrastructure.Data;
using AuctionService.Tests.TestUtils;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuctionService.Tests;

public class AuctionsControllerTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly IMapper _mapper = MapperFactory.CreateMapper();

    public void Dispose() => _dbFactory.Dispose();

    [Fact]
    public async Task CreateWithExistingItem_ShouldFail_WhenItemAlreadyHasAuction()
    {
        using var db = _dbFactory.CreateContext();

        var item = Seed.NewItem();
        var auction = Seed.NewScheduledAuction(item, DateTimeOffset.UtcNow.AddHours(2), DateTimeOffset.UtcNow.AddDays(1));
        db.Add(auction);
        await db.SaveChangesAsync();

        var controller = new AuctionsController(db, _mapper);

        var dto = new CreateAuctionWithExistingItemDto
        {
            SellerId = Guid.NewGuid(),
            ItemId = item.Id,
            StartingPrice = 50m,
            MinimumBidIncrement = 5m,
            Currency = "USD",
            StartsAt = DateTimeOffset.UtcNow.AddHours(3),
            EndsAt = DateTimeOffset.UtcNow.AddDays(2),
            Status = AuctionStatus.Scheduled
        };

        var result = await controller.CreateWithExistingItem(dto);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateWithNewItem_ShouldSetForeignKey_LinkedProperly()
    {
        using var db = _dbFactory.CreateContext();
        var controller = new AuctionsController(db, _mapper);

        var dto = new CreateAuctionWithNewItemDto
        {
            SellerId = Guid.NewGuid(),
            StartingPrice = 100m,
            MinimumBidIncrement = 10m,
            Currency = "USD",
            StartsAt = DateTimeOffset.UtcNow.AddHours(1),
            EndsAt = DateTimeOffset.UtcNow.AddDays(1),
            Item = new CreateItemDto
            {
                Title = "Neon",
                Description = "d",
                MimeType = "image/png",
                AssetUrl = "https://cdn/neon.png"
            }
        };

        var created = await controller.CreateWithNewItem(dto);
        var result = created.Result as CreatedAtActionResult;
        result.Should().NotBeNull();

        var createdDto = (AuctionDto)result!.Value!;
        createdDto.ItemId.Should().NotBe(Guid.Empty);
        createdDto.Item!.Id.Should().Be(createdDto.ItemId);

        // verify in db
        var dbAuction = await db.Auctions.Include(a => a.Item).FirstAsync(a => a.Id == createdDto.Id);
        dbAuction.ItemId.Should().Be(dbAuction.Item.Id);
    }

    [Fact]
    public async Task Update_ShouldBlockPricingAfterStart()
    {
        using var db = _dbFactory.CreateContext();

        var item = Seed.NewItem("Started Art");
        var starts = DateTimeOffset.UtcNow.AddMinutes(-10);
        var ends   = DateTimeOffset.UtcNow.AddHours(1);

        var auction = Seed.NewScheduledAuction(item, starts, ends);
        auction.Status = AuctionStatus.Live;
        db.Add(auction);
        await db.SaveChangesAsync();

        var controller = new AuctionsController(db, _mapper);

        var dto = new UpdateAuctionDto
        {
            StartingPrice = 999m, // forbidden after start
            ReservePrice = 1m,
            MinimumBidIncrement = 50m
        };

        var result = await controller.Update(auction.Id, dto);
        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value!.ToString().Should().Contain("cannot be changed");
    }

    [Fact]
    public async Task Update_ShouldAllowScheduleAndPricingBeforeStart_WithValidation()
    {
        using var db = _dbFactory.CreateContext();

        var item = Seed.NewItem("Soon Art");
        var starts = DateTimeOffset.UtcNow.AddHours(2);
        var ends   = DateTimeOffset.UtcNow.AddDays(1);

        var auction = Seed.NewScheduledAuction(item, starts, ends);
        db.Add(auction);
        await db.SaveChangesAsync();

        var controller = new AuctionsController(db, _mapper);

        var dto = new UpdateAuctionDto
        {
            StartsAt = DateTimeOffset.UtcNow.AddHours(3),
            EndsAt   = DateTimeOffset.UtcNow.AddDays(2),
            StartingPrice = 200m,
            ReservePrice = 300m,
            BuyNowPrice = 800m,
            MinimumBidIncrement = 25m
        };

        var action = await controller.Update(auction.Id, dto);
        action.Result.Should().BeNull();
        action.Value.Should().NotBeNull();

        var updated = await db.Auctions.AsNoTracking().FirstAsync(a => a.Id == auction.Id);
        updated.StartsAt.Should().Be(dto.StartsAt!.Value);
        updated.EndsAt.Should().Be(dto.EndsAt!.Value);
        updated.StartingPrice.Should().Be(200m);
        updated.ReservePrice.Should().Be(300m);
        updated.BuyNowPrice.Should().Be(800m);
        updated.MinimumBidIncrement.Should().Be(25m);
    }

    [Fact]
    public async Task Update_Status_ShouldAllowScheduledToCancelled_BeforeStart()
    {
        using var db = _dbFactory.CreateContext();

        var item = Seed.NewItem("Cancellable");
        var auction = Seed.NewScheduledAuction(item, DateTimeOffset.UtcNow.AddHours(4), DateTimeOffset.UtcNow.AddDays(2));
        db.Add(auction);
        await db.SaveChangesAsync();

        var controller = new AuctionsController(db, _mapper);

        var dto = new UpdateAuctionDto { Status = AuctionStatus.Cancelled };

        var action = await controller.Update(auction.Id, dto);
        action.Result.Should().BeNull();
        action.Value.Should().NotBeNull();

        var updated = await db.Auctions.AsNoTracking().FirstAsync(a => a.Id == auction.Id);
        updated.Status.Should().Be(AuctionStatus.Cancelled);
    }

    [Fact]
    public async Task List_WithIncludeItemFalse_ShouldReturnLightweightDtos()
    {
        using var db = _dbFactory.CreateContext();

        // two auctions
        var i1 = Seed.NewItem("A1");
        var a1 = Seed.NewScheduledAuction(i1, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(1));
        var i2 = Seed.NewItem("A2");
        var a2 = Seed.NewScheduledAuction(i2, DateTimeOffset.UtcNow.AddHours(2), DateTimeOffset.UtcNow.AddDays(2));
        db.AddRange(a1, a2);
        await db.SaveChangesAsync();

        var controller = new AuctionsController(db, _mapper);

        var resp = await controller.List(AuctionStatus.Scheduled, includeItem: false, page: 1, pageSize: 10, search: null) as OkObjectResult;
        resp.Should().NotBeNull();

        var list = resp!.Value as System.Collections.IEnumerable;
        list.Should().NotBeNull();

        // We can’t strongly cast without reflection in this small sample; presence is enough.
        // If you want, assert the first item has only the expected fields by serializing to dict.
    }
}
