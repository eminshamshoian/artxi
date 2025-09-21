using System;
using System.Threading.Tasks;
using AutoMapper;
using AuctionService.Api.Controllers;
using AuctionService.Api.DTOs;
using AuctionService.Domain.Entities;
using AuctionService.Infrastructure.Data;
using AuctionService.Tests.TestUtils;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuctionService.Tests;

public class ItemsControllerTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly IMapper _mapper = MapperFactory.CreateMapper();

    public void Dispose() => _dbFactory.Dispose();

    [Fact]
    public async Task Delete_ShouldBlock_WhenItemHasAuction()
    {
        using var db = _dbFactory.CreateContext();

        var item = Seed.NewItem();
        var auction = Seed.NewScheduledAuction(item, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(3));
        db.Add(auction); // adds item via nav as well
        await db.SaveChangesAsync();

        var controller = new ItemsController(db, _mapper);

        var result = await controller.Delete(item.Id);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<string>()
            .Which.Should().Contain("linked to an auction");
    }

    [Fact]
    public async Task Update_ShouldOnlyChangeAllowedFields_AndPublishDraft()
    {
        using var db = _dbFactory.CreateContext();

        // Item starts as Draft
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Title = "Draft Art",
            Description = "d",
            MimeType = "image/png",
            AssetUrl = "https://cdn/draft.png",
            Status = Domain.Enums.ItemStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();

        var controller = new ItemsController(db, _mapper);

        var dto = new UpdateItemDto
        {
            Title = "Updated Title",
            PreviewUrl = "https://cdn/prev.png",
            ThumbnailUrl = "https://cdn/thumb.png",
            Publish = true
        };

        var action = await controller.Update(item.Id, dto);
        var ok = action.Result as OkObjectResult;
        ok.Should().NotBeNull();

        var updated = await db.Items.AsNoTracking().FirstAsync(i => i.Id == item.Id);
        updated.Title.Should().Be("Updated Title");
        updated.PreviewUrl.Should().Be("https://cdn/prev.png");
        updated.ThumbnailUrl.Should().Be("https://cdn/thumb.png");
        updated.Status.Should().Be(Domain.Enums.ItemStatus.Published);
        updated.PublishedAt.Should().NotBeNull();

        // Ensure immutable content stayed the same
        updated.AssetUrl.Should().Be("https://cdn/draft.png");
        updated.MimeType.Should().Be("image/png");
    }
}
