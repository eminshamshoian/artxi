using AutoMapper;
using AuctionService.Api.DTOs;
using AuctionService.Domain.Entities;

namespace AuctionService.Api.Mappings;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        // ===== Read models =====
        // Auction -> AuctionDto (includes nested Item)
        CreateMap<Auction, AuctionDto>()
            .ForMember(d => d.ItemId, o => o.MapFrom(s => s.ItemId))
            .ForMember(d => d.Item,   o => o.MapFrom(s => s.Item));

        // Auction -> AuctionListItemDto (lightweight list/grid model)
        CreateMap<Auction, AuctionListItemDto>()
            .ForMember(d => d.Title,        o => o.MapFrom(s => s.Item.Title))
            .ForMember(d => d.ThumbnailUrl, o => o.MapFrom(s => s.Item.ThumbnailUrl));

        // Item -> ItemDto
        CreateMap<Item, ItemDto>();

        // ===== Create models =====
        // CreateItemDto -> Item
        CreateMap<CreateItemDto, Item>()
            // server-controlled or immutable fields should not be set from client
            .ForMember(d => d.Id,            o => o.Ignore())
            .ForMember(d => d.CreatedAt,     o => o.Ignore())
            .ForMember(d => d.PublishedAt,   o => o.Ignore())
            .ForMember(d => d.Auction,       o => o.Ignore());

        // CreateAuctionWithNewItemDto -> Auction (+ nested Item)
        CreateMap<CreateAuctionWithNewItemDto, Auction>()
            .ForMember(d => d.Id,                  o => o.Ignore())
            .ForMember(d => d.ItemId,              o => o.Ignore()) // set in controller from Item.Id
            .ForMember(d => d.CurrentHighBid,      o => o.Ignore())
            .ForMember(d => d.SoldAmount,          o => o.Ignore())
            .ForMember(d => d.WinnerId,            o => o.Ignore())
            .ForMember(d => d.WinnerDisplayName,   o => o.Ignore())
            .ForMember(d => d.CreatedAt,           o => o.Ignore())
            .ForMember(d => d.UpdatedAt,           o => o.Ignore())
            .ForMember(d => d.RowVersion,          o => o.Ignore())
            .ForMember(d => d.Item,                o => o.MapFrom(s => s.Item));

        // CreateAuctionWithExistingItemDto -> Auction (Item loaded separately)
        CreateMap<CreateAuctionWithExistingItemDto, Auction>()
            .ForMember(d => d.Id,                  o => o.Ignore())
            .ForMember(d => d.Item,                o => o.Ignore()) // controller assigns existing Item
            .ForMember(d => d.WinnerId,            o => o.Ignore())
            .ForMember(d => d.WinnerDisplayName,   o => o.Ignore())
            .ForMember(d => d.CreatedAt,           o => o.Ignore())
            .ForMember(d => d.UpdatedAt,           o => o.Ignore())
            .ForMember(d => d.RowVersion,          o => o.Ignore())
            .ForMember(d => d.CurrentHighBid,      o => o.Ignore())
            .ForMember(d => d.SoldAmount,          o => o.Ignore());

        // ===== Update models =====
        // Intentionally NO map from UpdateAuctionDto or UpdateItemDto to entities.
        // Updates are applied manually in controllers to enforce business rules
        // (immutables, state transitions, timing constraints, etc.).
    }
}
